FROM ubuntu:22.04

ENV DEBIAN_FRONTEND noninteractive

RUN \
    apt update && \
    apt install -y \
    # X Server
    xvfb \
    # VNC Server
    x11vnc \
    # Openbox
    openbox menu \
    # Other dependencies
    git && \
    # NoVNC
    git clone https://github.com/novnc/noVNC /opt/noVNC && \
    git clone https://github.com/novnc/websockify /opt/noVNC/utils/websockify && \
    # Clean up the apt cache
    apt clean && \
    rm -rf /var/lib/apt/lists/*

RUN \
    # Prepare: Install basic calculator and wget
    apt update && \
    apt upgrade -y && \
    apt install bc wget && \
    # Prepare: switch to 32 bit and add Wine key
    dpkg --add-architecture i386 && \
    mkdir -pm755 /etc/apt/keyrings && \
    wget -O /etc/apt/keyrings/winehq-archive.key https://dl.winehq.org/wine-builds/winehq.key && \
    # Downnload repository
    wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources && \
    # Update package and install Wine
    apt update && \
    apt upgrade -y && \
    apt install -y --install-recommends winehq-stable && \
    # Clean up the apt cache
    apt clean && \
    rm -rf /var/lib/apt/lists/*

RUN wget https://download.mql5.com/cdn/web/metaquotes.software.corp/mt4/mt4ubuntu.sh -O /mt4ubuntu.sh && \
    wget https://download.mql5.com/cdn/web/metaquotes.software.corp/mt5/mt5ubuntu.sh -O /mt5ubuntu.sh
RUN chmod u+x /mt4ubuntu.sh /mt5ubuntu.sh

CMD \
    # X Server
    Xvfb :1 -screen 0 1280x720x24 & \
    # Openbox
    (export DISPLAY=:1 && openbox-session) & \
    # VNC Server
    if [ -z $VNC_PASSWD ]; then \
    # no password
    x11vnc -display :1 -xkb -forever & \
    else \
    # set password from VNC_PASSWD env variable
    mkdir ~/.x11vnc && \
    x11vnc -storepasswd $VNC_PASSWD /root/.x11vnc/passwd && \
    x11vnc -display :1 -xkb -forever -rfbauth /root/.x11vnc/passwd & \
    fi && \
    # NoVNC
    if [ ! -e /root/noVNC.pem ]; then openssl req -new -x509 -days 36500 -nodes -batch -out /root/noVNC.pem -keyout /root/noVNC.pem; fi && \
    ln -s /opt/noVNC/vnc.html /opt/noVNC/index.html && \
    /opt/noVNC/utils/novnc_proxy --vnc localhost:5900 --listen 0.0.0.0:6080 --cert /root/noVNC.pem --ssl-only
