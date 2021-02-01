<template>
    <b-container fluid class="home">
        <b-row>
            <h1>{{ title }}</h1>
        </b-row>
        <b-row>
            <h2><i>{{ subtitle }}</i></h2>
        </b-row>
        <hr />
        <b-row>
            <b-button ref="btnToggle" 
                      block 
                      squared 
                      :variant="toggleColor" 
                      @click="toggleServer" 
                      :pressed.sync="serverToggle">Start Server</b-button>
        </b-row>
        <hr />
        <b-row>
            <h3>Server Log</h3>
        </b-row>
        <b-row>
            <textarea class="log"
                      block
                      ref="txtServerLog"
                      no-resize
                      rows="4"
                      placeholder="No messages found..."
                      :value="serverLog"></textarea>
        </b-row>
        <b-row>
            <b-button ref="btnClearLog" 
                      squared 
                      variant="primary"
                      @click="clearLog">Clear Log</b-button>
        </b-row>
    </b-container>
</template>

<script>
    export default {
        name: 'Home',
        mounted() {
            // check if server is running and run toggle method if needed
        },
        props: {
            title: String,
            subtitle: String
        },
        data: function () {
            return {
                serverToggle: false,
                toggleColor: "success",
                serverLog: ""
            }
        },
        methods: {
            toggleServer() {
                if (this.serverToggle) {
                    this.serverLog += '^ Started: ' + new Date().toLocaleString() + '\n';
                    this.$refs.btnToggle.innerText = "Server Stop";
                    this.toggleColor = "danger";
                } else {
                    this.serverLog += 'v Stopped: ' + new Date().toLocaleString() + '\n';
                    this.$refs.btnToggle.innerText = "Server Start";
                    this.toggleColor = "success";
                }
            },
            clearLog() {
                this.serverLog = "";
            }
        }
    };
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
    .log {
        background: #0a0a0a;
        color: #ffffff;
        width: 100%;
    }
</style>

