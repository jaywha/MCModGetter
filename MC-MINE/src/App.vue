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

    export default {
        name: 'app',
        mounted() {
            document.body.classList.add("mainBody");
        },
        components: {
            Home
        }
    };
</script>

<style>
    .mainBody {
        background: #000000;
        color: #00ff21;
    }
</style>

