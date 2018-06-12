docker run -it --name wiremock-container \
  -p 8080:8080 \
  -v $PWD/test2:/home/wiremock \
  -u $(id -u):$(id -g) \
  rodolpheche/wiremock \
    --proxy-all="https://api.ht.studsib.ru/" \
    --record-mappings --verbose
