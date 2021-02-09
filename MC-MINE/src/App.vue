<template>
    <div id="app">
        <Home 
              title="Minecraft: MINE" 
              subtitle="Mod Injection and Network Environment"
        />
    </div>
</template>

<script>
    import Home from './components/Home.vue';
    import Server from './components/Server.vue';

    export default {
        name: 'app',
        mounted() {
            document.body.classList.add("mainBody");
        },
        components: {
            Home,
            Server
        }
    };
</script>

<style>
    .mainBody {
        background: #000000;
        color: #00ff21;
    }
</style>

