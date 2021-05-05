// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import React from 'react';
import './App.css';
import * as microsoftTeams from '@microsoft/teams-js';
import HashParameters from './HashParameters';

class CloseIdTokenPopup extends React.Component {
  componentDidMount() {
    microsoftTeams.initialize();

    const hashParameters = new HashParameters();

    //The Azure implicit grant flow injects the result into the window.location.hash object. Parse it to find the results.
    let hashParams = hashParameters.getHashParameters();

    //If id token has been successfully granted it will be returned.
    if (hashParams['id_token']) {
      microsoftTeams.authentication.notifySuccess(hashParams['id_token']);
    } else {
      microsoftTeams.authentication.notifyFailure('Get id token failed');
    }
  }

  render() {
    return (
      <div>
        <h1>Get id token flow complete.</h1>
      </div>
    );
  }
}

export default CloseIdTokenPopup;
