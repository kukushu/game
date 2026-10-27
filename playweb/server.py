from __future__ import annotations

import argparse
import json
import os
import threading
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlsplit

from .engine import Session

STATIC = Path(__file__).with_name('static')
APP_ID = 'cityeconomylab-play'
MAX_BODY = 16 * 1024 * 1024


class Conflict(ValueError):
    pass


class Application:
    def __init__(self, save_path=None, session=None):
        self.lock = threading.RLock()
        self.save_path = Path(save_path) if save_path else None
        self.revision = 1
        if session is not None:
            self.session = session
        elif self.save_path and self.save_path.exists():
            data = json.loads(self.save_path.read_text(encoding='utf-8'))
            self.session = Session.load(data)
            self.revision = max(1, int(data.get('web_revision',1)))
        else:
            self.session = Session.new()

    def state(self):
        with self.lock:
            return {'revision':self.revision, **self.session.view()}

    def dump(self):
        return {'web_revision':self.revision, **self.session.dump()}

    def persist(self, session, revision):
        if self.save_path:
            self.save_path.parent.mkdir(parents=True,exist_ok=True)
            temporary=self.save_path.with_suffix('.tmp')
            temporary.write_text(json.dumps({'web_revision':revision,**session.dump()},ensure_ascii=False,allow_nan=False),encoding='utf-8')
            temporary.replace(self.save_path)

    def mutate(self, operation, data):
        allowed = {
            'advance': {'revision','count'}, 'action': {'revision','action','node','note'},
            'new': {'revision','seed','preset'}, 'anchor': {'revision'}, 'load': {'revision','save'},
        }
        with self.lock:
            if operation not in allowed or set(data)-allowed[operation]:
                raise ValueError('请求含有不支持的字段；不能直接修改统计或账户')
            if data.get('revision') != self.revision:
                raise Conflict('城市状态已更新，请刷新后重新操作')
            # A failed action, simulation, load, or disk write cannot partially overwrite a game.
            if operation=='new':
                candidate=Session.new(data.get('seed',1),data.get('preset','neighborhood'))
            elif operation=='load':
                if not isinstance(data.get('save'),dict): raise ValueError('存档应是 JSON 对象')
                candidate=Session.load(data['save'])
            else:
                candidate=Session.load(self.session.dump())
                if operation=='advance': candidate.advance(data.get('count'))
                elif operation=='action': candidate.intervene(data.get('action'),data.get('node'),data.get('note',''))
                elif operation=='anchor': candidate.anchor()
            self.persist(candidate,self.revision+1)
            self.session=candidate
            self.revision+=1
            return self.state()


class Server(ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self,address,app):
        self.app=app
        super().__init__(address,Handler)


class Handler(BaseHTTPRequestHandler):
    server_version = 'CityEconomyLabPlay/1'

    def log_message(self, format, *args):
        print(f'{self.address_string()} {format % args}',flush=True)

    def send(self, status, body, content_type='application/json; charset=utf-8', filename=None):
        if not isinstance(body,bytes):
            body=json.dumps(body,ensure_ascii=False,allow_nan=False).encode('utf-8')
        self.send_response(status)
        self.send_header('Content-Type',content_type)
        self.send_header('Content-Length',str(len(body)))
        self.send_header('Cache-Control','no-store')
        self.send_header('X-Content-Type-Options','nosniff')
        self.send_header('X-Frame-Options','DENY')
        self.send_header('Content-Security-Policy',"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'")
        if filename: self.send_header('Content-Disposition',f'attachment; filename="{filename}"')
        self.end_headers()
        try:
            self.wfile.write(body)
        except (BrokenPipeError,ConnectionResetError):
            pass

    def trusted(self):
        port=self.server.server_port
        hosts={f'127.0.0.1:{port}',f'localhost:{port}'}
        return self.headers.get('Host') in hosts and self.headers.get('Origin') in (None,*[f'http://{h}' for h in hosts])

    def do_GET(self):
        if not self.trusted():
            return self.send(403,{'error':'仅允许本机同源访问'})
        parsed=urlsplit(self.path)
        try:
            if parsed.path=='/api/health':
                return self.send(200,{'app':APP_ID,'pid':os.getpid(),'version':1})
            if parsed.path=='/api/state':
                return self.send(200,self.server.app.state())
            if parsed.path=='/api/save':
                with self.server.app.lock:
                    return self.send(200,self.server.app.dump(),filename=f'city-economy-seed-{self.server.app.session.player.seed}.json')
            if parsed.path=='/api/inspect':
                q=parse_qs(parsed.query)
                with self.server.app.lock:
                    detail=self.server.app.session.inspect(q.get('kind',['node'])[0],int(q.get('id',['0'])[0]),q.get('world',['player'])[0])
                    return self.send(200,{'revision':self.server.app.revision,**detail})
            files={'/':('index.html','text/html; charset=utf-8'),'/app.js':('app.js','text/javascript; charset=utf-8'),
                   '/style.css':('style.css','text/css; charset=utf-8'),'/favicon.svg':('favicon.svg','image/svg+xml')}
            if parsed.path in files:
                name,mime=files[parsed.path]
                return self.send(200,(STATIC/name).read_bytes(),mime)
            return self.send(404,{'error':'没有这个页面'})
        except (ValueError,KeyError,TypeError,AssertionError,IndexError) as error:
            return self.send(400,{'error':str(error) or '请求数据不合法'})

    def do_POST(self):
        if not self.trusted(): return self.send(403,{'error':'仅允许本机同源操作'})
        if self.headers.get('Content-Type','').split(';')[0] != 'application/json':
            return self.send(415,{'error':'请使用 application/json'})
        try:
            size=int(self.headers.get('Content-Length','0'))
            if not 0<size<=MAX_BODY: return self.send(413,{'error':'请求或存档过大'})
            data=json.loads(self.rfile.read(size))
            if not isinstance(data,dict): raise ValueError('请求应为 JSON 对象')
            if self.path=='/api/stop':
                self.send(200,{'stopped':True})
                threading.Thread(target=self.server.shutdown,daemon=True).start()
                return
            paths={'/api/advance':'advance','/api/action':'action','/api/new':'new','/api/anchor':'anchor','/api/load':'load'}
            if self.path not in paths: return self.send(404,{'error':'未知操作'})
            return self.send(200,self.server.app.mutate(paths[self.path],data))
        except Conflict as error:
            return self.send(409,{'error':str(error)})
        except (ValueError,KeyError,TypeError,AssertionError,IndexError,RecursionError) as error:
            return self.send(400,{'error':str(error) or '数据校验失败，原城市未改变'})
        except OSError:
            return self.send(500,{'error':'保存失败，原城市未改变；请检查本地存档目录是否可写'})


def main():
    parser=argparse.ArgumentParser(description='Run the local Python economy play prototype')
    parser.add_argument('--port',type=int,default=18765)
    parser.add_argument('--state',default='outputs/web/session.json')
    parser.add_argument('--browser',action='store_true')
    args=parser.parse_args()
    app=Application(args.state)
    with Server(('127.0.0.1',args.port),app) as server:
        url=f'http://127.0.0.1:{server.server_port}/'
        print(f'CityEconomyLab Play ready: {url}',flush=True)
        print('Time advances only when the player requests it.',flush=True)
        if args.browser: webbrowser.open(url)
        try: server.serve_forever()
        except KeyboardInterrupt: pass


if __name__=='__main__':
    main()
