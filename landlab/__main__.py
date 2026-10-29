import argparse
from pathlib import Path
import webbrowser

from playweb.server import Application, Server
from .session import LandSession
from .game_session import GameSession

parser=argparse.ArgumentParser(description='Land rights and autonomous location play lab')
parser.add_argument('--port',type=int,default=18766)
parser.add_argument('--state',default='outputs/land/session.json')
parser.add_argument('--browser',action='store_true')
parser.add_argument('--legacy-pool',action='store_true',help='Use historical research defaults instead of integrating employment on load')
args=parser.parse_args()
app=Application(args.state,session_type=LandSession if args.legacy_pool else GameSession,static=Path(__file__).with_name('static'),app_id='cityeconomylab-land',default_preset='blank',max_body=64*1024*1024)
with Server(('127.0.0.1',args.port),app) as server:
    url=f'http://127.0.0.1:{server.server_port}/'
    print(f'CityEconomyLab Land ready: {url}',flush=True)
    if args.browser: webbrowser.open(url)
    try: server.serve_forever()
    except KeyboardInterrupt: pass
