# REST API to KeeKee

The user interface is all implemented as HTML web pages that talk
to the KeeKee viewer through REST API requests.

The user inteface pages are served from specific directories
that are included with the KeeKee application

## Static Pages (`/static/`)

The URL `/static/` is the basis for static pages to support the
web interface. This directory includes things like common scripts,
common libraries, and basic server parameters.

These pages are served from the `web/static/` directory in the distribution.

## User Interface (`/UI/`) Pages

URL's beginning with `/UI/` are set for serving the actual
user interface web pages. Internally, the URL is modified
with a "skin" parameter which allows changing the look
and feel of the user interface.

For instance, if the "skin" name was "Dark", the URL `/UI/chat.html`
would be processed to access the directory `web/UI/Dark/chat.html` --
the "skin" name is inserted into the mapping from the URL
to the directory.

As is mentioned, the pages are served from the `web/UI/{SKIN}/`
directory. The passed in URL is cleaned up (directory navigation
removed, URI character encoding resolved, ...) and then is
used to select the file to serve.

The default "skin" is "Default".

The MIME types returned depend on the filename's extension.
The extensions processed are:`.htm`, `.html`, `.css`, `.js`,
`.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.ico`,
`.json`, and `.xml`.

## Authorization

Access to all the APIs is controlled by a bearer token included
in the `Authorization:` HTTP header. The access token is passed
to the View when it is initialized.

Sometime there might be a need for more complex authorization
like if there are helper or extension applications that run
beside KeeKee are created. Something for the future.

## Control APIs

Most of the control of KeeKee, the view contents, and the world
session is presented as a REST interface. These are the connection
between the web pages in the UI and KeeKee.

### KeeKee Application Control

- `/api/skin` - GET/POST to get/set "skin" to be used for UI
- `/api/stats/Renderer` - GET for renderer statistics
- `/api/stats/WorkQueue` - GET for work queue statistics
- `/api/stats/Comm` - GET for various communication statistics

### Session Control

- `/api/status` - GET session status

### OpenSimulator (LLLP) Specific

- `/api/LLLP/login` - GET/POST to get info for login and to do the login to an LLLP session
- `/api/LLLP/logout` - POST to logout
- `/api/LLLP/exit` - POST to cause logout and exit of application
- `/api/LLLP/teleport` - POST to teleport to new world location
- `/api/LLLP/status` - GET connection and session information