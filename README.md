![alt text](https://aam75.visualstudio.com/ChatService_GitHub/_apis/build/status/GitHub%20MasterBuild)
![alt text](https://aam75.vsrm.visualstudio.com/_apis/public/Release/badge/e3016cd0-189a-4aca-8603-e3b3bacfb83c/1/1)

# ChatService
ASP.NET Core Back-end Service for a Chat Application written in C#.
## It demonstrates knowledge of:
* Microservices:
  * Works with another SignalR Notification Service
  * Notification Service is Open-Source (https://github.com/alimans3/ChatNotificationService)
* Cloud No-SQL Storage:
  * Works with Azure Table Storage.
  * Tables Partitioned in efficient way.
* Metrics and Logging:
  * Uses EventFlow and Azure Application Insights to track performance and errors
* Countinous Integration/Countinous Delivery:
  * Uses Azure DevOps Pipelines for Build and Release to Azure Web Service
* Testing:
  * Includes MSTest projects that include unit and integration tests for the service.
  * Has 100% coverage for controllers and stores.
  * Uses coverlet and ReportGenerator for Code Coverage.
* Version Control:
  * Project is open-source and available on GitHub.
* RESTful APIs:
  * Provides clear APIs for profiles, messages, and conversations.
  * Supports Paging
* Front-End:
  * I have built a naive Xamarin Forms mobile chat application for testing.
  * It is open-source (https://github.com/alimans3/ChatApplication)
