## Docker Guide

This guide provides instructions on how to use Docker with the dotnet-workflow-engine project.

### Prerequisites

* Docker installed on your system
* dotnet-workflow-engine project cloned from GitHub

### Building the Docker Image

To build the Docker image, navigate to the project root and run the following command:

dotnet build -c Release

docker build -t dotnet-workflow-engine:latest .

### Running the Docker Container

To run the Docker container, use the following command:

docker run -p 8080:8080 dotnet-workflow-engine:latest

### Environment Variables

The following environment variables can be set to customize the behavior of the container:

* ASPNETCORE_URLS: The URL that the container will listen on. Default is http://+:8080.
* WORKFLOW_ENGINE_DB_CONNECTION_STRING: The connection string to the database. Default is an in-memory database.

### Docker Compose

To use Docker Compose, create a docker-compose.yml file in the project root with the following content:

version: '3'
services:
  api:
    build: .
    ports:
      - "8080:8080"
    depends_on:
      - db
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - WORKFLOW_ENGINE_DB_CONNECTION_STRING=Server=db;Database=WorkflowEngine;User Id=sa;Password=P@ssw0rd;
  db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - SA_PASSWORD=P@ssw0rd
      - ACCEPT_EULA=Y

Then, run the following command to start the containers:

docker-compose up

### Production Deployment

To deploy the container to a production environment, you can use a container orchestration tool like Kubernetes. Create a deployment YAML file with the following content:

apiVersion: apps/v1
kind: Deployment
metadata:
  name: dotnet-workflow-engine
spec:
  replicas: 1
  selector:
    matchLabels:
      app: dotnet-workflow-engine
  template:
    metadata:
      labels:
        app: dotnet-workflow-engine
    spec:
      containers:
      - name: dotnet-workflow-engine
        image: dotnet-workflow-engine:latest
        ports:
        - containerPort: 8080

Then, apply the YAML file to your Kubernetes cluster:

kubectl apply -f deployment.yaml

### Checklist

* Build the Docker image
* Run the Docker container
* Set environment variables as needed
* Use Docker Compose for development
* Deploy to production using Kubernetes

By following this guide, you can successfully use Docker with the dotnet-workflow-engine project.