import argparse, sys, os, uuid

parser = argparse.ArgumentParser(description="Apply a template to a file or directory")
parser.add_argument("path", default=".", help="The path to the root directory or file", metavar="PATH")
parser.add_argument("--var", "-v", dest="vars", action="append", nargs=2, help="Set a variable for find/replace", metavar=("NAME", "VALUE"))
parser.add_argument("--guid", dest="vars", action="append_const", const=("guid", str(uuid.uuid4())), help="Set the special \"guid\" variable to a random GUID")

args = parser.parse_args(sys.argv[1:])
kv = dict(args.vars)

def interpolate_contents(path):
    try:
        with open(path, "r+", encoding="utf-8") as f:
            content = f.read()
            interpolated_content = content.format(**kv)

            if content != interpolated_content:
                f.seek(0)
                f.write(interpolated_content)
                f.truncate()
    except UnicodeDecodeError:
        print("Ignored non-UTF8 file \"%s\"" % path)

def interpolate_name(path):
    head, tail = os.path.split(path.rstrip("/\\"))
    interpolated_tail = tail.format(**kv)

    if tail != interpolated_tail:
        os.rename(path, os.path.join(head, interpolated_tail))

for root, dirs, files in os.walk(args.path, topdown=False):
    for f in files:
        path = os.path.join(root, f)
        interpolate_contents(path)
        interpolate_name(path)

    for d in dirs:
        interpolate_name(os.path.join(root, d))
