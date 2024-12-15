# Document-Managment-System
A Document management system for archiving documents in a filestorage, with automatic OCR (queue for OC-recognition), tagging and full text search (ElasticSearch)

## Startup
To start up the application, the docker-compose.yml has to be executed by the command: "docker compose up" (A docker daemon has to be running for this to work). The Application can then be accessed by navigating to the localhost-port 80.
## Usage
The Document Management System is an application built on a Microservice-architecture used to **upload** and **manage** documents. It is further used to search documents by **content**. 
## Structure
The Document Management System consists of 8 Docker-Containers:
- **dms_rest_api**: Implements the RestAPI and the routing for the Document Management System.
- **webui**: Implements a simple user interface on localhost-port 80.
- **db**: Contains a postgresql database used for data-persistence.
- **rabbitmq**: Implements 3 message-queues to manage the workflow.
- **minio**: A service handling file-upload and storage.
- **ocr_worker**: A Tesseract ocr-worker to extract text from a picture.
- **elasticsearch**: A worker to handle searchrequests.
- **kibana**: A helperservice to visualize the elasticsearch container.

## HowTo - Integration Tests
- Make sure WSL is installed.
- Make sure Jq is installed in WSL ("sudo apt-get install jq").
- Make sure the Test.pdf PDF-file is also located in the same directory as the IntegrationTests.sh script (default location in the project folder).
- Open folder in wich IntegrationTests.sh and Test.pdf are located with WSL.
- Make sure all containers of the tested system are up an running.
- Type "./IntegrationTests.sh" into the WSL-console.
- Check results of integration tests in the WSL-console.

## HowTo - Db Migration:
- change Host parameter in appsettings.json of DMS_DAL to "localhost"
- start only db container
- open terminal in IDE
- navigate to DMS_DAL directory
- type "dotnet ef migrations add <migration-name>" into the terminal
- type "dotnet ef database update" into the terminal
- change "Host" parameter in appsettings.json of DMS_DAL back to "host.docker.internal" in order to enable the system itself to connect to the db
- restart system (maybe new build with "docker-compose build --no-cache" if there are caching problems)



  

