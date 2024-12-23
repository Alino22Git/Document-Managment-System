﻿using RabbitMQ.Client;
using DMS_REST_API.DTO;
using System.Text.Json;
using System.Text;
using DMS_REST_API.Exceptions;
using DMS_REST_API.Services;
using Microsoft.Extensions.Logging;

public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly IConnection _connection; // RabbitMQ connection
    private readonly IModel _channel; // channel for communicating with RabbitMQ
    private readonly ILogger<RabbitMQPublisher> _logger;
    private const string ExchangeName = "document_events"; // name for our message exchange

    // Log message templates
    private const string DocumentEventTemplate = "Document event {EventType} published for document {Id}";
    private const string DocumentEventErrorTemplate = "Failed to publish {EventType} event for document {Id}";

    public RabbitMQPublisher(ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq", // Stellen Sie sicher, dass dies mit Ihrem Docker-Service-Namen übereinstimmt
            Port = 5672,           // Standard-AMQP-Port
            UserName = "guest",
            Password = "guest"
        };

        try
        {
            // create connection and channel
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);

            // create queues for each event type
            _channel.QueueDeclare("document.created", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("document.updated", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("document.deleted", durable: true, exclusive: false, autoDelete: false);

            // bind queues to exchange with routing keys
            _channel.QueueBind("document.created", ExchangeName, "created");
            _channel.QueueBind("document.updated", ExchangeName, "updated");
            _channel.QueueBind("document.deleted", ExchangeName, "deleted");

            _logger.LogInformation("RabbitMQ connection initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            throw new MessageQueueConnectionException("Failed to initialize RabbitMQ connection", ex);
        }
    }

    public IConnection GetConnection()
    {
        if (_connection.IsOpen)
        {
            return _connection;
        }
        else
        {
            throw new InvalidOperationException("RabbitMQ connection is closed.");
        }
    }

    public void PublishDocumentCreated(DocumentDto document)
    {
        try
        {
            // Convert document to JSON and then to byte array for transmission over RabbitMQ
            var message = JsonSerializer.Serialize(document);
            var body = Encoding.UTF8.GetBytes(message);

            // Set message as persistent
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: "created",
                basicProperties: properties,
                body: body);

            _logger.LogInformation(DocumentEventTemplate, "created", document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, DocumentEventErrorTemplate, "created", document.Id);
            throw new MessagePublishException("Failed to publish document created event", ex);
        }
    }

    public void PublishDocumentUpdated(DocumentDto document)
    {
        try
        {
            var message = JsonSerializer.Serialize(document);
            var body = Encoding.UTF8.GetBytes(message);

            // Set message as persistent
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: "updated",
                basicProperties: properties,
                body: body);

            _logger.LogInformation(DocumentEventTemplate, "updated", document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, DocumentEventErrorTemplate, "updated", document.Id);
            throw new MessagePublishException("Failed to publish document updated event", ex);
        }
    }

    public void PublishDocumentDeleted(int documentId)
    {
        try
        {
            var message = JsonSerializer.Serialize(new { Id = documentId });
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: "deleted",
                basicProperties: properties,
                body: body);

            _logger.LogInformation(DocumentEventTemplate, "deleted", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, DocumentEventErrorTemplate, "deleted", documentId);
            throw new MessagePublishException("Failed to publish document deleted event", ex);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
