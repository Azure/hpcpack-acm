import { environment as defaultEnvironment } from './environment';
import { Observable } from 'rxjs/Observable';

export const environmentLoader = Observable.create((observer) => {

    var xmlhttp = new XMLHttpRequest(),
        method = 'GET',
        url = './assets/environments/environment.json';

    xmlhttp.open(method, url, true);

    xmlhttp.onload = function () {
        if (xmlhttp.status === 200) {
            let env = {};
            let res = JSON.parse(xmlhttp.responseText);
            env['production'] = res.production || defaultEnvironment.production;
            env['apiBase'] = res.apiBase || defaultEnvironment.apiBase;
            observer.next(env);
        } else {
            observer.next(defaultEnvironment);
        }
        observer.complete();
    };
    xmlhttp.send();
});
