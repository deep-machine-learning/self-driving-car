FROM bfolkens/docker-opencv:3.1.0-cuda8.0-cudnn5
#Needed separate to enable gcc-4.9 install
RUN sudo apt-get update -y
RUN sudo apt-get install software-properties-common python-software-properties -yq
RUN sudo add-apt-repository ppa:ubuntu-toolchain-r/test

RUN apt-get update -y
RUN apt-get install --assume-yes build-essential cmake git \
	build-essential pkg-config unzip qtbase5-dev python-dev python3-dev python-numpy python3-numpy\
	libopencv-dev libgtk-3-dev libdc1394-22 libdc1394-22-dev libjpeg-dev libpng12-dev libtiff5-dev libjasper-dev\
	libavcodec-dev libavformat-dev libswscale-dev libxine2-dev libgstreamer0.10-dev libgstreamer-plugins-base0.10-dev\
	libv4l-dev libtbb-dev libmp3lame-dev libopencore-amrnb-dev libopencore-amrwb-dev libtheora-dev\
	libvorbis-dev libxvidcore-dev v4l-utils openssl libssl-dev pkg-config autotools-dev autoconf libtool gcc-4.9 g++-4.9 \
	&& apt-get clean
RUN update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-4.9 60 --slave /usr/bin/g++ g++ /usr/bin/g++-4.9

# Install OpenCV
RUN echo "/usr/local/lib" > /etc/ld.so.conf.d/opencv.conf
RUN ldconfig

# Install restbed
RUN cd /usr/local/lib ; git clone --recursive https://github.com/corvusoft/restbed.git
RUN mkdir /usr/local/lib/restbed/build
RUN cd /usr/local/lib/restbed/build ; cmake [-DBUILD_TESTS=YES] [-DBUILD_EXAMPLES=YES] [-DBUILD_SSL=NO] [-DBUILD_SHARED=YES] [-DCMAKE_INSTALL_PREFIX=/output-directory] ..
RUN cd /usr/local/lib/restbed/build ; make -j $(($(nproc) + 1)) install

# Install jansson
RUN git clone https://github.com/akheron/jansson.git
RUN cd jansson ; autoreconf -fi
RUN cd jansson ; ./configure
RUN cd jansson ; make
RUN cd jansson ; make check
RUN cd jansson ; make install

#Deploy App
WORKDIR /restful-yolo/object-detection
RUN mkdir -p data/labels
COPY object-detection/Makefile /restful-yolo/object-detection
COPY object-detection/yolo.weights /restful-yolo/object-detection
COPY object-detection/src/ /restful-yolo/object-detection/src
COPY object-detection/data/voc.names /restful-yolo/object-detection/data
COPY object-detection/data/labels/ /restful-yolo/object-detection/data/labels
COPY object-detection/cfg/yolo-voc.cfg /restful-yolo/object-detection/cfg/
COPY object-detection/cfg/voc.data /restful-yolo/object-detection/cfg/
RUN echo "/usr/local/lib" > /etc/ld.so.conf.d/usr_local.conf
RUN /sbin/ldconfig
RUN make darknet-cpp

EXPOSE 1984
ENTRYPOINT ["./darknet-cpp","detector","load","cfg/voc.data","cfg/yolo-voc.cfg","yolo.weights"]