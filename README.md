# PCSX2 Upscaler

## Info

I created this tool to upscale the texture dump created by PCSX2 using wifu2x-caffe
This tool does not include wifu2x-caffe so you'll have to aquire that yourself
The application is currently looking for the exe file of wifu2x-caffe in the c: disc
c:/waifu2x-caffe/waifu2x-caffe-cui.exe
this will obviously be changed later but as of right now while its being heavily developed this is how it'll be for now.

Wifu2x-caffe: https://github.com/lltcggie/waifu2x-caffe

The mean of upscaling will be changed at a later date, as of rightnow its still early in development, total of 5 hours of development and i'd like to develop this further!

## Features

The application will run endlessly untill you stop it, reason for this is due to PCSX2 dumping files as levels load, so running this from the dump directory it will automatically pick up the new files added and process them.
This will be a setting that the user can change this behavior as they please, but for now you can stop it with the stop button!
