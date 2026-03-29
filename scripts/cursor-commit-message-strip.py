# Body for: py -m git_filter_repo --message-callback scripts/cursor-commit-message-strip.py
# Strips Cursor co-author trailers and "Made-with: Cursor" footers from commit/tag messages.
import re

text = message.decode('utf-8', 'replace')
lines = text.split('\n')
out = []
for line in lines:
	s = line.strip()
	if re.match(r'^Co-authored-by:\s*Cursor\s*<cursoragent@cursor\.com>\s*$', s, re.I):
		continue
	if re.match(r'^Made-with:\s*Cursor\s*$', s, re.I):
		continue
	out.append(line)
while out and out[-1] == '':
	out.pop()
body = '\n'.join(out)
if body:
	body += '\n'
return body.encode('utf-8')
