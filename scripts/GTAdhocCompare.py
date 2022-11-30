#/usr/bin/env python3
import argparse, re, subprocess
from difflib import HtmlDiff
from typing import List

#NEW_FILE = "D:\\git\\GTAdhocScripts\\projects\\gt5\\arcade\\ArcadeProjectComponent.ad.diss"
#ORIG_FILE = "D:\\gtmodding\\GT5VOL_211\\projects\\gt5\\arcade\\arcade.ad.diss"

RE_VERSION = r"Version: (\d*)"
RE_ROOT_INSTRUCTIONS = r"Root Instructions: (\d*)"
RE_STACK_SETUP = r"Stack Size: (\d*) - Variable Heap Size: (\d*) - Variable Heap Size Static: (\d*)"
RE_LEAVE = r"\| LEAVE:.*"
RE_INSTRUCTION = r"\d*\| *\d*\| *\d*\| *(.*)"
RE_INSTRUCTION_COMPONENTS_TO_DROP = r", (?:Index:|Local:|Static:|PushAt:)(\d*)"
RE_INSTRUCTION_JUMP = r"(?:Jump To Func Ins |Jump(?:To)?=)(\d*)"

HTML_STYLING = """
<style type="text/css">
    .diff {font-size: 12px;}
    body {background: #202124; color:#D6D6D6;}
    .diff_header {background-color:#252526 !important;}
    .diff_next {background-color:#333333 !important;}
    .diff_add {background-color:#339933 !important;}
    .diff_chg {background-color:#CCCC00 !important; color: #000;}
    .diff_sub {background-color:#993333 !important;}
    a {color:#AAAAFF}
</style>
"""

##########
# helpers

def error(str:str):
    print(f"[E] {str}")

def warn(str:str):
    print(f"[W] {str}")

def check_re(regex:str, prettyName:List[str], shouldError:bool=False):
    global newfile, origfile
    new_re = re.search(regex, newfile)
    orig_re = re.search(regex, origfile)
    for i in range(len(new_re.groups())):
        if new_re.group(i+1) != orig_re.group(i+1):
            if shouldError:
                error(f"Mismatched {prettyName[i]}: {new_re.group(i+1)} new / {orig_re.group(i+1)} orig")
            else:
                warn(f"Mismatched {prettyName[i]}: {new_re.group(i+1)} new / {orig_re.group(i+1)} orig")

    newfile = newfile[new_re.end():]
    origfile = origfile[orig_re.end():]

##########
# main

parser = argparse.ArgumentParser(
    description="Compares two .adc files. "+\
        "Usually used with one original PDI file, "+\
        "and one reverse engineered and GTAdhocCompiler compiled file."
)
parser.add_argument("new_file", help="Reverse engineered file (.ad.diss, .adc, .ad)")
parser.add_argument("original_file", help="Original PDI file (.ad.diss, .adc)")
parser.add_argument("output_file", nargs='?', help="Output HTML file (default is 'comparison.html')")
parser.add_argument("-L", "--limiter", type=int, help="Amount of line difference to limit (useful for testing while writing)")
parser.add_argument("-j", "--showjump", action="store_true", help="When set, doesn't obfuscate jump instructions (can cause lots of 'differences' due to LEAVE instructions)")
parser.add_argument("-l", "--showleave", action="store_true", help="When set, leaves LEAVE instructions in the output (will cause a lot of 'differences').")
out = parser.parse_args()
NEW_FILE = out.new_file # type: str
ORIG_FILE = out.original_file # type: str

if NEW_FILE.endswith(".ad"):
    try:
        subprocess.run(
            ["adhoc.exe", "build", "-i", NEW_FILE],
            capture_output=True,
            check=True,
        )
        print("Ran adhoc.exe to turn 'new_file' .ad into a .adc")
    except FileNotFoundError:
        print("==> When providing an .ad file, adhoc.exe must be on the $PATH or in cwd.")
        exit(1)
    NEW_FILE = NEW_FILE[:-3]+".adc"

if NEW_FILE.endswith(".adc"):
    try:
        subprocess.run(
            ["adhoc.exe", NEW_FILE],
            capture_output=True,
            check=True,
        )
        print("Ran adhoc.exe to turn 'new_file' .adc into a .ad.diss")
    except FileNotFoundError:
        print("==> When providing an .adc (or .ad) file, adhoc.exe must be on the $PATH or in cwd.")
        exit(1)
    NEW_FILE = NEW_FILE[:-4]+".ad.diss"

if ORIG_FILE.endswith(".adc"):
    try:
        subprocess.run(
            ["adhoc.exe", ORIG_FILE],
            capture_output=True,
            check=True,
        )
        print("Ran adhoc.exe to turn 'original_file' .adc into a .ad.diss")
    except FileNotFoundError:
        print("==> When providing an .adc file, adhoc.exe must be on the $PATH or in cwd.")
        exit(1)
    ORIG_FILE = ORIG_FILE[:-4]+".ad.diss"

newfile = ""
with open(NEW_FILE, "r", encoding= 'utf-8') as f:
    newfile = f.read()

origfile = ""
with open(ORIG_FILE, "r", encoding= 'utf-8') as f:
    origfile = f.read()

check_re(RE_VERSION, ["version"], True)
check_re(RE_ROOT_INSTRUCTIONS, ["root instruction count"], True)

newlines = newfile.split("\n")
origlines = origfile.split("\n")

newlines2 = []
origlines2 = []

for line in newlines:
    # any line with an instruction which is not a leave
    re_instr = re.search(RE_INSTRUCTION, line)
    if (line == "" or re_instr is None):
        continue
    if ((not out.showleave) and re.search(RE_LEAVE, line) is not None):
        continue

    line2 = re.sub(RE_INSTRUCTION_COMPONENTS_TO_DROP, "", re_instr.group(1))
    re_jump = re.search(RE_INSTRUCTION_JUMP, line2)
    if re_jump is not None and out.showjump is False:
        line2 = re.sub(RE_INSTRUCTION_JUMP, f"Jump:UNK", line2) # re_jump.group(1)
    
    newlines2.append(line2)

newlines = newlines2

for line in origlines:
    # any line with an instruction which is not a leave
    re_instr = re.search(RE_INSTRUCTION, line)
    if (line == "" or re_instr is None):
        continue
    if ((not out.showleave) and re.search(RE_LEAVE, line) is not None):
        continue

    line2 = re.sub(RE_INSTRUCTION_COMPONENTS_TO_DROP, "", re_instr.group(1))
    re_jump = re.search(RE_INSTRUCTION_JUMP, line2)
    if re_jump is not None and out.showjump is False:
        line2 = re.sub(RE_INSTRUCTION_JUMP, f"Jump:UNK", line2) # re_jump.group(1)

    origlines2.append(line2)

origlines = origlines2

if out.limiter is not None:
    if len(newlines) > len(origlines) + out.limiter:
        newlines = newlines[:len(origlines) + out.limiter]
    elif len(origlines) > len(newlines) + out.limiter:
        origlines = origlines[:len(newlines) + out.limiter]

print("Building comparison...")

differ = HtmlDiff(4)
html = differ.make_file(origlines, newlines, ORIG_FILE, NEW_FILE)
with open(out.output_file or 'comparison.html', "w", encoding= 'utf-8') as f:
    html = html.replace('<style type="text/css">', HTML_STYLING+'<style type="text/css">')
    f.write(html)

print(f"Built {out.output_file or 'comparison.html'}")
