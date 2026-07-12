// This file is used by Karma to bootstrap the Angular testing environment.
import 'zone.js';
import 'zone.js/testing';

import { getTestBed } from '@angular/core/testing';
import { BrowserDynamicTestingModule, platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing';

getTestBed().initTestEnvironment(BrowserDynamicTestingModule, platformBrowserDynamicTesting());

// Load all the spec files
declare const require: any;
const context = require.context('./', true, /\.spec\.ts$/);
context.keys().map(context);

export {};
