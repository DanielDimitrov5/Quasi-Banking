import { startConsumer, shutdownConsumer } from './consumer';

console.log('🚀 Starting Notification Service...');
startConsumer();

const gracefulShutdown = async () => {
    console.log('\nSIGTERM received, shutting down gracefully...');
    await shutdownConsumer();
    process.exit(0);
};

process.on('SIGINT', gracefulShutdown);
process.on('SIGTERM', gracefulShutdown);
