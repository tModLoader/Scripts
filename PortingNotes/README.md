# PortingNotes/
Scripts for approximate conversion of `#dev-update-log`/`#preview-update-log` channel posts into Steam & Discord monthly update posts. 

### Requirements
- .NET
- `dotnet script` - Install via `dotnet tool install -g dotnet-script`.

### Usage
1. Drag and drop a text file full of Discord message contents (TEMP: use a bot to get them, or ask Mirsario) onto `PortingNotesParser.bat`. 
2. Copy and rename the newly appeared/updated `PortingNotesParser_Output.hjson` file, to today's date for example.
3. TEMP: Fill in `DiscordMessageUrl` fields in the copied file with message URLs such as
    > `https://discord.com/channels/XXXXXXXXXXXXXXXXXX/YYYYYYYYYYYYYYYYYY/ZZZZZZZZZZZZZZZZZZZ`.
4. Drag and drop the file you modified onto `PortingNotesConverter.bat`. This will autogenerate `PortingNotesConverter_Discord.md` and `PortingNotesConverter_Steam.txt`.
