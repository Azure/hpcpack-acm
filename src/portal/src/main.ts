import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';
import { environmentLoader } from './environments/environmentLoader';


environmentLoader.subscribe(env => {
  if (env.production) {
    enableProdMode();
  }
  environment.production = env.production;
  environment.apiBase = env.apiBase;
  platformBrowserDynamic().bootstrapModule(AppModule).catch(err => console.log(err));;
});