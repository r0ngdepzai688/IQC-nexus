/* eslint-disable */
const fs = require('fs');
const path = require('path');

function walk(dir, callback) {
  fs.readdirSync(dir).forEach(f => {
    let dirPath = path.join(dir, f);
    let isDirectory = fs.statSync(dirPath).isDirectory();
    isDirectory ? walk(dirPath, callback) : callback(dirPath);
  });
}

walk('src', (filePath) => {
  if (filePath.endsWith('.tsx') || filePath.endsWith('.ts')) {
    let content = fs.readFileSync(filePath, 'utf8');
    let original = content;

    // Primary Colors
    content = content.replace(/bg-\[#1428A0\]/g, 'bg-primary');
    content = content.replace(/text-\[#1428A0\]/g, 'text-primary');
    content = content.replace(/border-\[#1428A0\]/g, 'border-primary');
    content = content.replace(/from-\[#1428A0\]/g, 'from-primary');
    content = content.replace(/ring-\[#1428A0\]/g, 'ring-primary');
    content = content.replace(/focus:border-\[#1428A0\]/g, 'focus:border-primary');
    
    // Dark mode primary overrides
    content = content.replace(/dark:bg-\[#2A44D4\]/g, 'dark:bg-primary');
    content = content.replace(/dark:text-\[#2A44D4\]/g, 'dark:text-primary');

    // Backgrounds / Surfaces Light
    content = content.replace(/bg-\[#F8F9FA\]/g, 'bg-muted');
    content = content.replace(/bg-\[#F4F6F9\]/g, 'bg-background');

    // Backgrounds / Surfaces Dark
    content = content.replace(/dark:bg-\[#121826\]/g, 'dark:bg-card');
    content = content.replace(/dark:bg-\[#121212\]/g, 'dark:bg-card');
    content = content.replace(/dark:bg-\[#151515\]/g, 'dark:bg-popover');
    content = content.replace(/dark:bg-\[#1A1A1A\]/g, 'dark:bg-popover');
    content = content.replace(/dark:bg-\[#222\]/g, 'dark:bg-secondary');
    content = content.replace(/dark:bg-\[#111111\]/g, 'dark:bg-muted');
    content = content.replace(/dark:bg-\[#0A0A0A\]/g, 'dark:bg-background');
    content = content.replace(/dark:bg-\[#050505\]/g, 'dark:bg-background');
    
    // Borders
    content = content.replace(/border-gray-100 dark:border-white\/5/g, 'border-border');
    content = content.replace(/border-gray-200 dark:border-white\/5/g, 'border-border');
    content = content.replace(/border-gray-100 dark:border-white\/10/g, 'border-border');
    content = content.replace(/border-gray-200 dark:border-white\/10/g, 'border-border');
    content = content.replace(/dark:border-white\/5/g, 'border-border');
    content = content.replace(/dark:border-white\/10/g, 'border-border');
    
    // Duplicates
    content = content.replace(/border-border border-border/g, 'border-border');

    if (content !== original) {
      fs.writeFileSync(filePath, content, 'utf8');
      console.log('Updated: ' + filePath);
    }
  }
});
