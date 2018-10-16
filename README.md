# ChatService
Backend Web Application for a Chat Application

This app service provides the following APIs:
## Usage
### Profile:
#### Add new profile for users.
Use: api/profile 
'''
Body: 
'''
firstname:
'''
lastname:
'''
username:
'''

#### Get existing profile.
Use: api/profile/{username}



### Conversation:
#### Add new conversation between participants
Use: api/conversations
'''
Body: 
'''
participants:[user1,user2]

#### Get All conversations for specific user
Use: api/conversations/{username}



### Messages:
#### Add a message to a conversation
Use: api/conversation
'''
Body: 
'''
SenderUsername:
'''
Text:

#### Get all messages for specific conversations
Use: api/conversation/{conversationId}

## Storage:
This API gives two ways of storage:
'''
Azure Table Storage
'''
InMemory Temporary Storage
