export interface BankingEvent {
    EventId: string;
    EventType: string;
    Timestamp: string;
}

export interface AccountCreatedEvent extends BankingEvent {
    EventType: 'AccountCreatedEvent';
    AccountId: string;
    OwnerId: string;
    OwnerEmail: string;
    InitialDeposit: number;
}

export interface TransactionProcessedEvent extends BankingEvent {
    EventType: 'TransactionProcessedEvent';
    TransactionId: string;
    AccountId: string;
    Amount: number;
    Type: 'Deposit' | 'Withdrawal';
    Description: string;
}
