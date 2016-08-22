# Fate Replay Parser #

This is an automated tool for parsing Warcraft III Replay Files generated from playing Fate / Another III custom game.

### Dependencies ###

* .NET Framework 4.5 
* Database: MySQL 5.7.13
* ORM: Entity Framework 6

### How does it work? ###

Fate Replay Parser takes a w3g file and deciphers the data blocks based on WC3 Replay File Format Description (https://gist.github.com/dengzhp/1185519). While the information in this URL was a great starting point, the contents were very outdated and does not accurately depict the current version of WC3 replay file, so some guesswork and reversing WC3 engine was necessary to fully understand how data is written to a replay file.

In VJass, calling SyncStoredInteger on a gamecache writes a header value of 0x70 followed by the actual data being synced. Fate / Another III map makes use of this sync native call to record detailed metagame information into the replay file, such as servant selections, items purchased, KDA etc. 

Once the w3g file is parsed, then the detailed game information is written into the database. Fate Web Server queries this information to display a user-friendly statistics about the game that was played.