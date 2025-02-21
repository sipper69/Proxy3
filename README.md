# Proxy3

Use Control3 via a secure WebRTC session to remotely control your device over the internet.

Proxy3 will provide the CH9329 and Video access to Control3 via a secure WebRTC session.
This project is a proof of concept initiative, but again very usable :-)
Still I'm not a professional Dev, but I hope this repository will be inspiring. 

After quite some research around the available WebRTC libraries, I came to the conclusion a low level C++ approach is the way to go.
But... WebRTC is originally build by Google as a browser based realtime communication solution via a bunch of JavaScript API's. 
Nowadays supported by all popular web browser. So why not just use that native implementation?

webView2 is available per default in the Windows App SDK (WinUI3). So this little project is a pretty simple straight forward implementation of:
- 1 HTML page loaded in webView2 handling the WebRTC session via JavaScript. This includes the videosource and a datachannel for the CH9329 packets
- Main application to interact with the Javascript and presenting the HTML page in a window

Ably.com is used for the WebRTC signalling. 
Just create an account and an app which will provide you with an API key to be entered in Proxy3.

Because this is a peer-2-peer connection some firewalls may cause a challenge. 
Google provides STUN servers to support the most common peer-2-peer connection set up.
If default STUN is not working, TURN for traffic relay should be used, but this is a paid service of course (potentially a lot of video data!).
I added Twilio support for TURN traffic relay, they have a robust and affordable service.

Last but not least, Control3 version 2.0.0 or newer is needed to set up a connection with Proxy3. 
The Control3 repository on GitHub is not maintained. 
Control3 2.0.1 will be release on the Microsoft App Store soon...

Cheers


