// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import React from 'react';

import crypto from 'crypto';
import * as microsoftTeams from '@microsoft/teams-js';

/**
 * This component is used to redirect the user to the Azure authorization endpoint from a popup
 */
class IdTokenPopup extends React.Component {
  componentDidMount() {
    // Initialize the Microsoft Teams SDK
    microsoftTeams.initialize();

    // Get the user context in order to extract the tenant ID
    microsoftTeams.getContext(async (context: microsoftTeams.Context) => {
      let tenant = context['tid']; //Tenant ID of the logged in user
      let client_id = process.env.REACT_APP_AZURE_APP_REGISTRATION_ID; //Client ID of the Azure AD app registration ( may be from different tenant for multitenant apps)
      let queryParams: any = {
        tenant: tenant,
        client_id: client_id,
        response_type: 'id_token',
        scope: 'openid',
        redirect_uri: window.location.origin + '/id-token-end',
        nonce: crypto.randomBytes(16).toString('base64'),
        state: crypto.randomBytes(8).toString('base64'),
      };

      let url = `https://login.microsoftonline.com/${tenant}/oauth2/v2.0/authorize?`;
      queryParams = new URLSearchParams(queryParams).toString();

      let idTokenEndpoint = url + queryParams;

      window.location.assign(idTokenEndpoint);
    });
  }

  render() {
    return (
      <div>
        <h1>Redirecting to id token get page...</h1>
      </div>
    );
  }
}

export default IdTokenPopup;
