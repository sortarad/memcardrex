import pefile
import sys
pe = pefile.PE(sys.argv[1], fast_load=True)
pe.OPTIONAL_HEADER.Subsystem = 0x0002
pe.write(filename=sys.argv[1])