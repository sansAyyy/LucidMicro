import { createApp } from 'vue';

import App from './App.vue';
import { router } from './app/router';
import { pinia } from './app/stores';
import { installUi } from './app/ui';
import './shared/styles/base.css';
import './shared/styles/theme.css';

const app = createApp(App);

installUi(app);

app.use(pinia).use(router).mount('#app');
