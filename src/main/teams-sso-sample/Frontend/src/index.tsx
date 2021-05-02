// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './components/App';

// [2021-May-2nd] Fixed to accommodate breaking change in fluentui/react-northstar v0.50.0, as outlined in: https://github.com/microsoft/fluentui/pull/13268
import { Provider, teamsTheme } from '@fluentui/react-northstar'; //https://fluentsite.z22.web.core.windows.net/quick-start

ReactDOM.render(
  <React.StrictMode>
    <Provider theme={teamsTheme}>
      <App />
    </Provider>
  </React.StrictMode>,
  document.getElementById('root'),
);
