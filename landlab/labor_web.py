"""Compatibility entry: employment is now integrated into the original land game."""
import runpy

if __name__=='__main__':
    runpy.run_module('landlab',run_name='__main__')
