import { Kafka, EachMessagePayload } from 'kafkajs';
import { AccountCreatedEvent, TransactionProcessedEvent, BankingEvent } from './types';

const kafka = new Kafka({
    clientId: 'notification-service',
    brokers: [process.env.KAFKA_BROKERS || 'localhost:29092'],
});

const consumer = kafka.consumer({ groupId: 'notification-service-group' });

export const startConsumer = async () => {
    
    try {
        await consumer.connect();
        console.log('Notification Service: Connected to Kafka');

        await consumer.subscribe({ topic: 'banking-events', fromBeginning: true });
        console.log('Subscribed to "banking-events" topic');

        await consumer.run({
            eachMessage: async ({ topic, partition, message }: EachMessagePayload) => {
                if (!message.value) return;

                try {
                    const event = JSON.parse(message.value.toString()) as BankingEvent;
                    console.log(`📨 Received event: ${event.EventType}`);

                    switch (event.EventType) {
                        case 'AccountCreatedEvent':
                            handleAccountCreated(event as AccountCreatedEvent);
                            break;
                        case 'TransactionProcessedEvent':
                            handleTransactionProcessed(event as TransactionProcessedEvent);
                            break;
                        default:
                            console.warn(`Unknown event type: ${event.EventType}`);
                    }
                } catch (jsonError) {
                    console.error('Failed to parse message', jsonError);
                }
            },
        });
    } catch (error) {
        console.error('Failed to start consumer', error);
        // Implement retry logic if needed
        setTimeout(startConsumer, 5000);
    }
};

const handleAccountCreated = (event: AccountCreatedEvent) => {
    const message = `
    =========================================
    📧 Sending Welcome Email
    To: ${event.OwnerEmail}
    Subject: Welcome to the Bank!

    Your new account (${event.AccountId}) has been created with an initial deposit of $${event.InitialDeposit.toFixed(2)}.
    =========================================
    `;
    console.log(message);
};

const handleTransactionProcessed = (event: TransactionProcessedEvent) => {
    const action = event.Type === 'Deposit' ? 'deposited' : 'withdrawn';
    const amount = Math.abs(event.Amount);
    
    // In a real app, you would look up the user's email from the AccountId
    const recipientEmail = "user@example.com"; 
    
    const message = `
    ==========================================
     transactionalert
    To: ${recipientEmail} (for Account ID: ${event.AccountId})
    Subject: Transaction Alert

    An amount of $${amount.toFixed(2)} has been ${action}.
    Description: ${event.Description}
    ==========================================
    `;
    console.log(message);
};

export const shutdownConsumer = async () => {
    await consumer.disconnect();
    console.log('👋 Consumer disconnected');
};
