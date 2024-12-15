# Document-Managment-System
A Document management system for archiving documents in a FileStore, with automatic OCR (queue for OC-recognition), tagging and full text search (ElasticSearch)

## Startup
To start up the application the dockercompose.yml has to be executed by the command: "docker compose up". The Application can then be accessed by navigating to the localhost-port 80.
## Usage
The Document Management System is an Application built on a Microservice Architecture used to Upload and Manage Documents. It is further used to search Documents by Content. 
## Structure
The Document Management System consists of 8 Docker-Containers:
dms_rest_api: Implements the RestAPI and the Routing for the Document Management System.
webui: Implements a simple User Interface on localhost-port 80
db: Contains a postgres Database used for data-persistence
rabbitmq: Implements X Messagequeues to manage the workflow
minio: A Service handling File-upload and storage
ocr_worker: A Tesseract ocr-worker to extract text from a picture
elasticsearch: A worker to handle searchrequests
kibana: A helperservice to visualize the elasticsearch container

