<script lang="ts">
    import { toast } from '$lib/stores/toast';
    import { fade, fly } from 'svelte/transition';

    function getIcon(type: string) {
        switch (type) {
            case 'success':
                return '✓';
            case 'error':
                return '✕';
            case 'warning':
                return '⚠';
            case 'info':
                return 'ℹ';
            default:
                return 'ℹ';
        }
    }
</script>

<div class="toast-container">
    {#each $toast as item (item.id)}
        <div
            class="toast toast-{item.type}"
            transition:fly={{ y: -50, duration: 300 }}
            on:click={() => toast.dismiss(item.id)}
            role="alert"
        >
            <span class="toast-icon">{getIcon(item.type)}</span>
            <span class="toast-message">{item.message}</span>
            <button class="toast-close" on:click={() => toast.dismiss(item.id)}>×</button>
        </div>
    {/each}
</div>

<style>
    .toast-container {
        position: fixed;
        top: 1rem;
        left: 50%;
        transform: translateX(-50%);
        z-index: 9999;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        max-width: 400px;
    }

    .toast {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 1rem 1.25rem;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        cursor: pointer;
        transition: transform 0.2s;
        animation: slideIn 0.3s ease-out;
    }

    .toast:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    }

    @keyframes slideIn {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }

    .toast-success {
        background-color: #10b981;
        color: white;
    }

    .toast-error {
        background-color: #ef4444;
        color: white;
    }

    .toast-warning {
        background-color: #f59e0b;
        color: white;
    }

    .toast-info {
        background-color: #3b82f6;
        color: white;
    }

    .toast-icon {
        font-size: 1.5rem;
        font-weight: bold;
        flex-shrink: 0;
    }

    .toast-message {
        flex: 1;
        font-weight: 500;
    }

    .toast-close {
        background: none;
        border: none;
        color: white;
        font-size: 1.5rem;
        cursor: pointer;
        padding: 0;
        width: 24px;
        height: 24px;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 4px;
        transition: background-color 0.2s;
    }

    .toast-close:hover {
        background-color: rgba(255, 255, 255, 0.2);
    }

    @media (max-width: 640px) {
        .toast-container {
            left: 1rem;
            right: 1rem;
            max-width: none;
        }
    }
</style>
