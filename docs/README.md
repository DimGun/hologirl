# HoloGirl

Simple app that allows to record audio from device's mic and control character behavior.

## Server request
Server accepts uncompressed audio requests.
And returns adio response in the same uncompressed format.
```
curl -X POST  -H 'Cache-Control: no-cache' \
              -H 'Content-Type: application/x-www-form-urlencoded' \
              --data-binary @"request.wav" \
              http://studsib.ru:9999/receive  > response.wav
```
