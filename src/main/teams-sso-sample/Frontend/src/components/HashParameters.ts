class HashParameters {
  //Helper function that converts window.location.hash into a dictionary
  getHashParameters() {
    let hashParams: any = {};
    window.location.hash
      .substr(1)
      .split('&')
      .forEach(function (item) {
        let [key, value] = item.split('=');
        hashParams[key] = decodeURIComponent(value);
      });
    return hashParams;
  }
}

export default HashParameters;
