![alt text](https://aam75.visualstudio.com/ChatService_GitHub/_apis/build/status/GitHub%20MasterBuild)
![alt text](https://aam75.vsrm.visualstudio.com/_apis/public/Release/badge/e3016cd0-189a-4aca-8603-e3b3bacfb83c/1/1)

# ChatService
ASP.NET Core Backend Web Application for a Chat Application
## Installing
Add Azure Connection String to appSettings.json
This app service provides the following APIs:
## Usage
### Profile:
#### Add new profile for users.
* Use: api/profile 
* Body: 
  * firstname:
  * lastname:
  * username:

#### Get existing profile.
* Use: api/profile/{username}



### Conversation:
#### Add new conversation between participants
* Use: api/conversations
* Body: 
  * participants:[user1,user2]

#### Get All conversations for specific user
* Use: api/conversations/{username}



### Messages:
#### Add a message to a conversation
* Use: api/conversation
* Body: 
  * SenderUsername:
  * Text:

#### Get all messages for specific conversations
* Use: api/conversation/{conversationId}

## Storage:
This API gives two ways of storage:
* Azure Table Storage
* InMemory Temporary Storage

## Metrics:
* Using Application Insights and EventFlow

