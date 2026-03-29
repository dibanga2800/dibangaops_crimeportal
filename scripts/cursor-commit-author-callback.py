# Body for: py -m git_filter_repo --force --commit-callback scripts/cursor-commit-author-callback.py
TARGET_NAME = b"David Ibanga"
TARGET_EMAIL = b"47346863+dibanga2800@users.noreply.github.com"

_an = commit.author_name.lower()
_ae = commit.author_email.lower()
_author_cursor = (
	b"cursoragent" in _ae
	or _ae.endswith(b"@cursor.com")
	or _an == b"cursor"
	or _an == b"cursor agent"
	or (b"cursor" in _an and b"agent" in _an)
)
if _author_cursor:
	commit.author_name = TARGET_NAME
	commit.author_email = TARGET_EMAIL

_cn = commit.committer_name.lower()
_ce = commit.committer_email.lower()
_committer_cursor = (
	b"cursoragent" in _ce
	or _ce.endswith(b"@cursor.com")
	or _cn == b"cursor"
	or _cn == b"cursor agent"
	or (b"cursor" in _cn and b"agent" in _cn)
)
if _committer_cursor:
	commit.committer_name = TARGET_NAME
	commit.committer_email = TARGET_EMAIL
