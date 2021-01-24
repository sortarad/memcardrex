docker rmi -f memcardrex
docker rm -f memcardrex
docker build -t memcardrex --build-arg BUILD_NET=1.0.0.0 .
docker create --name memcardrex memcardrex
mkdir -p dist
docker cp memcardrex:/root/.wine/drive_c/Build/MemcardRex.msi dist/MemcardRex.msi 
docker cp memcardrex:/tmp/MemcardRex.dmg dist/MemcardRex.dmg 