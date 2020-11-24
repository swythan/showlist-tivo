# ShowList for TiVo®
This is the source code for my old [ShowList for TiVo®](http://www.windowsphone.com/s?appid=f19ef184-5017-4e10-bf57-9e48ecd1bbd5) Windows Phone app.

The Tivo.Connect projects contain the code for talking to the TiVo API (either on the internet, or directly to a local TiVo).
The TiVoAhoy projects contain the application. The application projects probably need an old phone SDK. 

There are also some test projects in here for testing the network comms and operating as a MITM proxy from the official app in order to work out the communication protocol.

__N.B. NONE__ of this will work without a valid client certificate that the TiVo API will accept to authenticate the initial TLS connection (and it's password). 
In the past I successfully extracted what was needed from the official apps, but the certificates I have expired long ago. 
