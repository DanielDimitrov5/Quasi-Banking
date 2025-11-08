<script lang="ts">
    import '../app.css';
    import Toast from '$lib/components/Toast.svelte';
</script>

<Toast />

<nav class="navbar">
    <div class="container">
        <h1>🏦 Banking App</h1>
        <div class="nav-links">
            <a href="/">Dashboard</a>
            <a href="/create-account">Create Account</a>
        </div>
    </div>
</nav>

<main class="container">
    <slot />
</main>

<style>
    .navbar {
        background-color: var(--dark-color);
        color: white;
        padding: 1rem 0;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        margin-bottom: 2rem;
    }

    .navbar .container {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .navbar h1 {
        font-size: 1.5rem;
        margin: 0;
    }

    .nav-links {
        display: flex;
        gap: 1.5rem;
    }

    .nav-links a {
        color: white;
        text-decoration: none;
        font-weight: 500;
        transition: opacity 0.2s;
    }

    .nav-links a:hover {
        opacity: 0.8;
    }
</style>
