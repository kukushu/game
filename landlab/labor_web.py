"""Separate labor-market play server. Never opens or migrates the user's land save."""
import argparse
from pathlib import Path
from playweb.server import Application, Server
from .session import LandSession


def main():
    parser=argparse.ArgumentParser(description='Isolated manufacturing employment experiment')
    parser.add_argument('--port',type=int,default=18767)
    parser.add_argument('--state',default='outputs/labor-play/session.json')
    args=parser.parse_args()
    app=Application(args.state,session_type=LandSession,static=Path(__file__).with_name('static'),
                    app_id='cityeconomylab-labor',default_preset='labor',max_body=64*1024*1024)
    with Server(('127.0.0.1',args.port),app) as server:
        print(f'CityEconomyLab Labor ready: http://127.0.0.1:{server.server_port}/',flush=True)
        try:server.serve_forever()
        except KeyboardInterrupt:pass


if __name__=='__main__':main()
