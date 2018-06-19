# RainCraft Rest API #

**GET /servers**

This list all launched and available instances. It returns a JSON file with this format:

    [
        {
            "name": "<server name>",
            "endpoints": {
                "minecraft": "<publicly available IP:port>",
                "rcon": "<publicly available IP:port>"
            }
        }
        ...
    ]

 The outer level array should be able to list multiple instances, each with its own name. 
 
 For our architecture, We anticipate the "minecraft" and "rcon" keyword will share the same external IP, but use a different port. "minecraft" ports ascend from 25565. "rcon" ports ascend from 25575.

**POST /servers/*name***

This launches a new server having the specified name. 

If the create is successful, it returns a response code of 201. The Location response-header field returns the server name, e.g.:  "Location: PodPeople". The content is empty.

If the create is unsuccessful, it returns a response code of 200.  An error is anticipated if the server name is already taken or if the server instance could not be launched. The content response is JSON:

    {
        "name": "<server name>",
        "error": "Diagnostic message"
    }

 
  **DELETE /servers/*name***

  This remove the server instance that has the specified name.
  
  If the named server exists and is successfully removed, it returns a response code of 204 and no content.

  If the named server does not exist or cannot be removed, it returns a response code of 200 and this JSON:

    {
        "name": "<server name>",
        "error": "Diagnostic message"
    }

