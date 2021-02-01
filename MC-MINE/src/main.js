import Vue from 'vue';
import BootstrapVue from "bootstrap-vue";
import App from './App.vue';
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap-vue/dist/bootstrap-vue.css"

Vue.config.productionTip = true;
Vue.use(BootstrapVue);

new Vue({
    render: h => h(App)
}).$mount('#app');
