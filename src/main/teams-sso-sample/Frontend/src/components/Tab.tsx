// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import React from 'react';
//import * as msal from '@azure/msal-browser';
import AuthenticationContext from 'adal-angular';
import './App.css';
import * as microsoftTeams from '@microsoft/teams-js';
import { Avatar, Loader } from '@fluentui/react-northstar';
// import crypto from 'crypto';

/**
 * This tab component renders the main tab content
 * of your app.
 */

export interface ITabProps {}
interface ITabState {
  context?: microsoftTeams.Context;
  ssoToken: string;
  consentRequired: boolean;
  consentProvided: boolean;
  graphAccessToken: string;
  adalAuthContext: AuthenticationContext | undefined;
  clientId: string;
  idToken: string;
  photo: string;
  error: boolean;
}
class Tab extends React.Component<ITabProps, ITabState> {
  constructor(props: ITabProps) {
    super(props);
    this.state = {
      context: undefined,
      ssoToken: '',
      consentRequired: false,
      consentProvided: false,
      graphAccessToken: '',
      idToken: '',
      adalAuthContext: undefined,
      clientId: '',
      photo: '',
      error: false,
    };

    //Bind any functions that need to be passed as callbacks or used to React components
    this.ssoLoginSuccess = this.ssoLoginSuccess.bind(this);
    this.ssoLoginFailure = this.ssoLoginFailure.bind(this);
    this.consentSuccess = this.consentSuccess.bind(this);
    this.consentFailure = this.consentFailure.bind(this);
    this.unhandledFetchError = this.unhandledFetchError.bind(this);
    //this.callGraphFromClient = this.callGraphFromClient.bind(this);
    this.getPhotoFromGraph = this.getPhotoFromGraph.bind(this);
    this.showConsentDialog = this.showConsentDialog.bind(this);
  }

  //React lifecycle method that gets called once a component has finished mounting
  //Learn more: https://reactjs.org/docs/react-component.html#componentdidmount
  componentDidMount() {
    // Initialize the Microsoft Teams SDK
    microsoftTeams.initialize();

    // Get the user context from Teams and set it in the state
    microsoftTeams.getContext((context: microsoftTeams.Context) => {
      this.setState({ context: context });
    });

    //Perform Azure AD single sign-on authentication
    let authTokenRequestOptions: microsoftTeams.authentication.AuthTokenRequest =
      {
        successCallback: (result: string) => {
          this.ssoLoginSuccess(result);
        }, //The result variable is the SSO token.
        failureCallback: (error: string) => {
          this.ssoLoginFailure(error);
        },
      };

    microsoftTeams.authentication.getAuthToken(authTokenRequestOptions);
  }

  ssoLoginSuccess = async (authToken: string) => {
    this.setState({ ssoToken: authToken });

    //await this.exchangeClientTokenForServerToken(authToken);
    await this.callApiEndpointWithConsentCheck(authToken, 'checkConsent');
    // await this.getIdToken();
  };

  ssoLoginFailure(error: string) {
    console.error('SSO failed: ', error);
    this.setState({ error: true });
  }

  //Exchange the SSO access token for a Graph access token
  //Learn more: https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow
  exchangeClientTokenForServerToken = async (authToken: string) => {
    let checkConsent = await this.callApiEndpointWithConsentCheck(
      authToken,
      'exchangeAccessToken',
    );

    if (checkConsent) {
      const hasConsent: boolean = checkConsent[0];
      const data = checkConsent[1];

      if (hasConsent) {
        this.setState({ graphAccessToken: data['access_token'] });
      } else {
        console.error(data);
        this.setState({ error: true });
      }
    }
  };

  callApiEndpointWithConsentCheck = async (
    token: string,
    endpointName: string,
  ) => {
    let requestHeaders = new Headers();
    requestHeaders.set('Authorization', 'Bearer ' + token);

    let serverURL = `${process.env.REACT_APP_BASE_URL}/api/${endpointName}`;
    console.log('here ' + serverURL);

    let response = await fetch(serverURL, { headers: requestHeaders }).catch(
      this.unhandledFetchError,
    );

    if (response) {
      let data = await response.json().catch(this.unhandledFetchError);

      if (!response.ok && data.error === 'consent_required') {
        //A consent_required error means it's the first time a user is logging into to the app, so they must consent to sharing their Graph data with the app.
        //They may also see this error if MFA is required.
        this.setState({ consentRequired: true }); //This displays the consent required message.
        this.showConsentDialog(); //Proceed to show the consent dialogue.
      } else if (!response.ok) {
        //Unknown error
        console.error(data);
      } else {
        //Server side token exchange worked. Save the access_token to state, so that it can be picked up and used by the componentDidMount lifecycle method.
        return [true, data];
      }
      return [false, data];
    }
  };

  //Show a popup dialogue prompting the user to consent to the required API permissions. This opens ConsentPopup.js.
  //Learn more: https://docs.microsoft.com/en-us/microsoftteams/platform/tabs/how-to/authentication/auth-tab-aad#initiate-authentication-flow
  showConsentDialog() {
    microsoftTeams.authentication.authenticate({
      url: window.location.origin + '/auth-start',
      width: 600,
      height: 535,
      successCallback: (result: string | undefined) => {
        this.consentSuccess(result ?? '');
      },
      failureCallback: (reason: string | undefined) => {
        this.consentFailure(reason ?? '');
      },
    });
  }

  //Callback function for a successful authorization
  consentSuccess(result: string) {
    //Save the Graph access token in state
    this.setState({
      graphAccessToken: result,
      consentProvided: true,
    });
  }

  consentFailure(reason: string) {
    console.error('Consent failed: ', reason);
    this.setState({ error: true });
  }

  getIdToken = async () => {
    const context = this.state.context;
    const client_id = process.env.REACT_APP_AZURE_APP_REGISTRATION_ID; //Client ID of the Azure AD app registration ( may be from different tenant for multitenant apps)
    this.setState({ clientId: client_id as string });

    let config = {
      clientId: client_id as string,
      redirectUri: (window.location.origin + '/id-token-end') as string, // redirectUri must be in the list of redirect URLs for the Azure AD app
      cacheLocation: 'sessionStorage',
      navigateToLoginRequestUrl: false as boolean,
    } as AuthenticationContext.Options;

    if (context?.loginHint) {
      config.extraQueryParameter =
        'scope=openid' +
        '&tenant=' +
        context?.tid +
        '&login_hint=' +
        encodeURIComponent(context?.loginHint);
    } else {
      config.extraQueryParameter = 'scope=openid&tenant=' + context?.tid;
    }

    var authContext = new AuthenticationContext(config);

    this.setState({ adalAuthContext: authContext });

    if (this.state.adalAuthContext) {
      const authContext = this.state.adalAuthContext;
      let user = authContext.getCachedUser();
      if (user) {
        if (user?.profile?.oid === undefined) {
          // User doesn't match, clear the cache
          authContext.clearCache();
        }
      }

      authContext.acquireToken(
        this.state.clientId,
        (errorDesc, token, error) => {
          if (!token) {
            console.log('Renewal failed: ' + error);
          }
        },
      );
    }
    //this.showIdTokenDialog();

    // microsoftTeams.getContext(async (context: microsoftTeams.Context) => {
    //   const client_id = process.env.REACT_APP_AZURE_APP_REGISTRATION_ID; //Client ID of the Azure AD app registration ( may be from different tenant for multitenant apps)
    //   const tenant = context['tid']; //Tenant ID of the logged in user
    //   let authority = `https://login.microsoftonline.com/${tenant}`;

    // Configure ADAL localstorage
    // let config = {
    //   clientId: 'g075edef-0efa-453b-997b-de1337c29185' as string,
    //   //redirectUri: window.location.origin + "/SilentAuthEnd",       // This should be in the list of redirect uris for the AAD app
    //   cacheLocation: 'localStorage' as CacheLocation,
    //   navigateToLoginRequestUrl: false,
    // };
    // let authContext = new AuthenticationContext(config);

    // var cfg: msal.Configuration = {
    //   auth: {
    //     clientId: client_id as string,
    //     authority: authority as string,
    //   },
    //   // cache: {
    //   //   cacheLocation: 'localStorage' as CacheLocation,
    //   // },
    // };

    // Configure MSAL localstorage
    // const msalConfig = {
    //   auth: {
    //     clientId: client_id as string,
    //     authority: authority as string,
    //   },
    //   cache: {
    //     cacheLocation: 'localStorage',
    //   },
    // };

    // const msalInstance = new msal.PublicClientApplication(msalConfig);

    // const sid = context.sessionId;

    // var request = {
    //   scopes: ['user.read'],
    //   loginHint: context.loginHint as string,
    // extraQueryParameters: { domain_hint: 'organizations' as string },

    //sid: context.sessionId,
    //};

    // var currentAccount = msalInstance.getAccountByUsername(
    //   context.loginHint as string,
    //);

    // await msalInstance
    //   .acquireTokenSilent(request)
    //   .then((response) => {
    //     const token = response.idToken;
    //   })
    //   .catch((error) => {
    //     const e = error;
    //   });

    // userAgentApplication
    //   .acquireTokenSilent(request)
    //   .then((response) => {
    //     const token = response.idToken;
    //   })
    //   .catch((error) => {
    //     const e = error;
    //   });

    // let tenant = context['tid']; //Tenant ID of the logged in user
    // let client_id = process.env.REACT_APP_AZURE_APP_REGISTRATION_ID; //Client ID of the Azure AD app registration ( may be from different tenant for multitenant apps)
    // let queryParams: any = {
    //   tenant: tenant,
    //   client_id: client_id,
    //   response_type: 'id_token',
    //   scope: 'openid',
    //   redirect_uri: window.location.origin + '/id-token-end',
    //   nonce: crypto.randomBytes(16).toString('base64'),
    //   state: crypto.randomBytes(8).toString('base64'),
    // };

    // let url = `https://login.microsoftonline.com/${tenant}/oauth2/v2.0/authorize?`;
    // queryParams = new URLSearchParams(queryParams).toString();

    // let idTokenEndpoint = url + queryParams;

    // let response = await fetch(idTokenEndpoint, {
    //   method: 'GET',
    // })
    //   .then((response) => {
    //     if (response.redirected) {
    //       window.location.assign(response.url);
    //     }
    //   })
    //   .catch(this.unhandledFetchError);

    // if (response) {
    //   let data = await response.json().catch(this.unhandledFetchError);
    //   let idToken = data['id_token'];
    // }

    // window.location.assign(idTokenEndpoint);
    // });
  };

  // Show a popup dialogue prompting the user to consent to the required API permissions. This opens ConsentPopup.js.
  // Learn more: https://docs.microsoft.com/en-us/microsoftteams/platform/tabs/how-to/authentication/auth-tab-aad#initiate-authentication-flow
  showIdTokenDialog() {
    microsoftTeams.authentication.authenticate({
      url: window.location.origin + '/id-token-start',
      width: 600,
      height: 535,
      successCallback: (result: string | undefined) => {
        this.idTokenSuccess(result ?? '');
      },
      failureCallback: (reason: string | undefined) => {
        this.idTokenFailure(reason ?? '');
      },
    });
  }

  //Callback function for a successful authorization
  idTokenSuccess(result: string) {
    //Save the Graph access token in state
    this.setState({
      idToken: result,
    });
  }

  idTokenFailure(reason: string) {
    console.error('Get id token failed: ', reason);
    this.setState({ error: true });
  }

  //React lifecycle method that gets called after a component's state or props updates
  //Learn more: https://reactjs.org/docs/react-component.html#componentdidupdate
  componentDidUpdate = async (prevProps: ITabProps, prevState: ITabState) => {
    //Check to see if a Graph access token is now in state AND that it didn't exist previously
    if (
      prevState.graphAccessToken === '' &&
      this.state.graphAccessToken !== ''
    ) {
      this.getPhotoFromGraph();
    }
  };

  // Fetch the user's profile photo from Graph using the access token retrieved either from the server
  // or microsoftTeams.authentication.authenticate
  // callGraphFromClient = async () => {
  //   let upn = this.state.context?.upn;
  //   let graphPhotoEndpoint = `https://graph.microsoft.com/v1.0/users/${upn}/photo/$value`;
  //   let graphRequestParams = {
  //     method: 'GET',
  //     headers: {
  //       'Content-Type': 'image/jpg',
  //       authorization: 'bearer ' + this.state.graphAccessToken,
  //     },
  //   };

  //   let response = await fetch(graphPhotoEndpoint, graphRequestParams).catch(
  //     this.unhandledFetchError,
  //   );
  //   if (response) {
  //     if (!response.ok) {
  //       console.error('ERROR: ', response);
  //       this.setState({ error: true });
  //     }

  //     let imageBlog = await response.blob().catch(this.unhandledFetchError); //Get image data as raw binary data

  //     this.setState({
  //       photo: URL.createObjectURL(imageBlog), //Convert binary data to an image URL and set the url in state
  //     });
  //   }
  // };

  // Fetch the user's profile photo from Graph via the server API.
  getPhotoFromGraph = async () => {
    let upn = this.state.context?.userPrincipalName;
    let graphPhotoEndpoint = `https://graph.microsoft.com/v1.0/users/${upn}/photo/$value`;
    let graphRequestParams = {
      method: 'GET',
      headers: {
        'Content-Type': 'image/jpg',
        authorization: 'bearer ' + this.state.graphAccessToken,
      },
    };

    let response = await fetch(graphPhotoEndpoint, graphRequestParams).catch(
      this.unhandledFetchError,
    );
    if (response) {
      if (!response.ok) {
        console.error('ERROR: ', response);
        this.setState({ error: true });
      }

      let imageBlog = await response.blob().catch(this.unhandledFetchError); //Get image data as raw binary data

      this.setState({
        photo: URL.createObjectURL(imageBlog), //Convert binary data to an image URL and set the url in state
      });
    }
  };

  //Generic error handler ( avoids having to do async fetch in try/catch block )
  unhandledFetchError(err: string) {
    console.error('Unhandled fetch error: ', err);
    this.setState({ error: true });
  }

  render() {
    let title =
      this.state.context && Object.keys(this.state.context).length > 0 ? (
        'Congratulations ' + this.state.context['upn'] + '! This is your tab'
      ) : (
        <Loader />
      );

    let ssoMessage =
      this.state.ssoToken === '' ? (
        <Loader label="Performing Azure AD single sign-on authentication..." />
      ) : null;

    let serverExchangeMessage =
      this.state.ssoToken !== '' &&
      !this.state.consentRequired &&
      this.state.photo === '' ? (
        <Loader label="Exchanging SSO access token for Graph access token..." />
      ) : null;

    let consentMessage =
      this.state.consentRequired && !this.state.consentProvided ? (
        <Loader label="Consent required." />
      ) : null;

    let avatar =
      this.state.photo !== '' ? (
        <Avatar image={this.state.photo} size="largest" />
      ) : null;

    let content;
    if (this.state.error) {
      content = (
        <h1>
          ERROR: Please ensure pop-ups are allowed for this website and retry
        </h1>
      );
    } else {
      content = (
        <div>
          <h1>{title}</h1>
          <h3>{ssoMessage}</h3>
          <h3>{serverExchangeMessage}</h3>
          <h3>{consentMessage}</h3>
          <h1>{avatar}</h1>
        </div>
      );
    }

    return <div>{content}</div>;
  }
}
export default Tab;
