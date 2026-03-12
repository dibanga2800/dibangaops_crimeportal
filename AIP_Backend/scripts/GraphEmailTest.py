import argparse
import datetime
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request


def load_config(config_path: str) -> dict:
    with open(config_path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def get_graph_settings(config: dict) -> dict:
    settings = config.get("GraphEmail", {})
    return {
        "tenant_id": settings.get("TenantId"),
        "client_id": settings.get("ClientId"),
        "client_secret": settings.get("ClientSecret"),
        "from_user": settings.get("FromUser"),
    }


def require_settings(settings: dict) -> None:
    missing = [key for key, value in settings.items() if not value]
    if missing:
        raise ValueError(f"Missing Graph settings: {', '.join(missing)}")


def fetch_access_token(tenant_id: str, client_id: str, client_secret: str) -> str:
    token_url = f"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token"
    payload = urllib.parse.urlencode(
        {
            "client_id": client_id,
            "client_secret": client_secret,
            "scope": "https://graph.microsoft.com/.default",
            "grant_type": "client_credentials",
        }
    ).encode()
    request = urllib.request.Request(token_url, data=payload)
    response = urllib.request.urlopen(request)
    data = json.loads(response.read())
    return data["access_token"]


def send_graph_email(access_token: str, from_user: str, to_user: str, subject: str, body: str) -> None:
    send_url = f"https://graph.microsoft.com/v1.0/users/{urllib.parse.quote(from_user)}/sendMail"
    payload = {
        "message": {
            "subject": subject,
            "body": {"contentType": "HTML", "content": body},
            "toRecipients": [{"emailAddress": {"address": to_user}}],
        },
        "saveToSentItems": True,
    }
    request = urllib.request.Request(
        send_url,
        data=json.dumps(payload).encode(),
        headers={"Authorization": f"Bearer {access_token}", "Content-Type": "application/json"},
    )
    urllib.request.urlopen(request)


def main() -> int:
    parser = argparse.ArgumentParser(description="Send a Microsoft Graph test email.")
    parser.add_argument("--to", required=True, help="Recipient email address.")
    parser.add_argument(
        "--config",
        default=os.path.join(os.path.dirname(__file__), "..", "appsettings.json"),
        help="Path to appsettings.json with GraphEmail config.",
    )
    args = parser.parse_args()

    config_path = os.path.abspath(args.config)
    config = load_config(config_path)
    settings = get_graph_settings(config)
    require_settings(settings)

    access_token = fetch_access_token(
        settings["tenant_id"], settings["client_id"], settings["client_secret"]
    )

    sent_at = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    subject = "AIP Graph Email Test"
    body = (
        "<h2>AIP Graph Email Test</h2>"
        "<p>This is a test email sent via Microsoft Graph.</p>"
        f"<p>Sent at: {sent_at}</p>"
    )

    send_graph_email(access_token, settings["from_user"], args.to, subject, body)
    print("Graph sendMail request completed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except urllib.error.HTTPError as exc:
        print(f"Graph sendMail failed: {exc.code}")
        try:
            print(exc.read().decode("utf-8"))
        except Exception:
            print(str(exc))
        raise SystemExit(1)
    except Exception as exc:
        print(f"Graph sendMail failed: {exc}")
        raise SystemExit(1)
