# Video Archive Bot

A telegram bot to archive videos of classes.

## Features

* No disk space used for videos. Only file ID's are stored in database
* Embedded; Sqlite is used as database. This means that you don't have to setup an external database for it.
* Admins and review queues. Users can freely submit videos to bot and admins can verify them, making them available to
  other users as well.
* Semi-Stateless. The only state used in bot is when users are uploading videos. Inline buttons do not use states.

## Usage

### Compiling

To compile and run the bot, you need .Net 6. Simply run

```bash
dotnet build --configuration Release
```

to compile the bot.

### Running

To run the bot, at first you have to set the environmental variable of `BOT_TOKEN` as your Telegram bot token. You can
also optionally, set `DB_PATH` to set a custom database path. Otherwise this is defaulted to `database.db`. Then just
run the program. Another option which can be used is `UPLOADER_PRIVACY` which can be either `none`, `name` or `all`. Any
other value is interpreted as `all`. This value defines if users can see the uploader name of videos.

After running the program send `/start` to bot to test it. Then you probably want to make yourself the admin of the bot.
It is recommended to at first stop the bot and then open the database of the bot. Then, go to users table and find
yourself and make yourself admin by changing value of column of `is_admin` to 1.
