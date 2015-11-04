# MMBot.Jenkins
A Jenkins plugin for MMBot

Harness the power of the [MMBot](http://github.com/mmbot/mmbot), a .Net port of Github's [Hubot](http://github.com/github/hubot) via your Jenkins CI server.

# Installation

Install MMBot.Jenkins as you would any other MMBot adapter. Visit the [MMBot Readme](https://github.com/mmbot/mmbot#getting-started) for more information.

# Configuration

You'll need at least the following configuration parameters in your mmbot.ini file:

```
[JENKINS]
USERNAME=jenkins user
APIKEY=jenkins api key
BASEURL=http://your.jenkins.instance/
BUILDTOKEN=jenkins build token
```
