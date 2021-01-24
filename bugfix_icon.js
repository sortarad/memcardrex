Object.defineProperty(process, 'arch', { value: 'x86' });

const rcedit = require('rcedit')
rcedit(process.argv[2], {
  icon: process.argv[3]
    });