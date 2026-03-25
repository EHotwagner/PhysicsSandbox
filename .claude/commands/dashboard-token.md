Extract the Aspire Dashboard browser token from the running AppHost and copy it to the system clipboard.

Run `./dashboard-token.sh --copy` to copy the token to clipboard. If that fails (no X11 display), fall back to printing the token so the user can copy it manually.

Also print the full dashboard login URL for convenience.

If the AppHost is not running, tell the user to start it with `./start.sh`.
