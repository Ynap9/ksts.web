import { IEnvironment } from '@/app/shared/models/environment.models';

export const environment: IEnvironment = {
    production: false,
    apiUrl: 'http://localhost:5009',
    appUrl: 'http://localhost:4200',
    authGrantType: 'password',
    authClientId: 'client-web',
    authClientSecret: 'mBSQUHmZ4be5bQYfhwS7hjJZ2zFOCU2e',
    authScope: 'openid offline_access',
    pluginUrl: 'http://127.0.0.1:17739'
};
