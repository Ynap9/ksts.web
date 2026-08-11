import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Dashboard } from './app/pages/dashboard/dashboard';
import { Documentation } from './app/pages/documentation/documentation';
import { Landing } from './app/pages/landing/landing';
import { Notfound } from './app/pages/notfound/notfound';
import { authGuard } from './app/shared/guard/auth-guard';
import { KySo } from './app/pages/ky-so/ky-so';

export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', component: KySo },
            { path: 'uikit', loadChildren: () => import('./app/pages/uikit/uikit.routes') },
            { path: 'documentation', component: Documentation },
            { path: 'pages', loadChildren: () => import('./app/pages/pages.routes') },
            { path: 'user-management', loadChildren: () => import('./app/pages/user-management/user-management.routes') },
            { path: 'template', loadChildren: () => import('./app/pages/template/template.routes') },
            { path: 'chung-thu-so', loadChildren: () => import('./app/pages/chung-thu-so/chung-thu-so.routes') },
            { path: 'import-tuyen-sinh', loadChildren: () => import('./app/pages/import-tuyen-sinh/import-tuyen-sinh.routes') },
            { path: 'ky-so', loadChildren: () => import('./app/pages/ky-so/ky-so.routes') },
        ]
    },
    { path: 'landing', component: Landing },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
