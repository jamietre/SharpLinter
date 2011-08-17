SHARPLINTER 0.99

(c) 2011 James Treworgy

### Code use license.

LICENSE (MIT License)
 
Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:
 
The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Summary

SharpLinter is a command line tool to automate error-checking Javascript files. It produces output that is formatted for 
Visual Studio's output window, so clicking on a line will locate the file and line in the IDE.

Each of the linting options can be specified in a configuration file as well (see below)

    
    SharpLinter [-f file.js] [-[r]d] /directory [-c sharplinter.conf] [-o options]
                [-j jslint.js] [-jr jslint | jshint] [-y] [-p yui | packer | best] [-ph] [-k]
                [-is text] [-ie text] [-if text]");

    Basic

    -i file.js                      Process file.js. This option can be specified multiple times.
    -d /directory/*.js              Process all files matching pattern *.js in /directory
    -rd /directory/*.js             Recurse subfolders to find more matches  
    -x *.min.js                     Exclude files that match *.min.js

    Configuring
    
    -j c:/jslint.js                 Use c:/jslint.js to perform linting instead of embedded JSHINT   
    -y                              Also run the file through Yahoo YUI compressor and report errors
    -c c:/sharplinter.conf          Load global lint configuration settings from c:/sharplinter.conf. Any 
                                    additional command-line settings or settings in the file will supercede
                                    the global settings.
    -is lint-ignorestart            Define /*lint-ignorestart*/ as the beginning of an ignore block
    -ie lint-ignoreend              Define /*lint-ignoreend*/ as the end of an ignore block
    -if lint-ignorefile             Define /*lint-ignorefile*/ as a flag to skip the entire file
    -jr jshint                      Assume that JSHint is being used for processing (other option: jslint).
                                    This should not be necessary, SharpLinter will try to figure out which you 
                                    are using from the code itself.
    
    Minimizing 
    
    -p yui|packer|best *.min.js     When no errors are found, minimize to filename.min.js using Yahoo YUI
                                    compressor, Dean Edwards' JSPacker (without data compression), or
                                    whichever produces a smaller file
    
    -ph                             Preserve the contents of the first content block of format /* ... */
                                    at the top of the minimized output. Must not have any non-whitespace
                                    before the opening comment tag.
    
     Miscellaneous
  
     -k                             Wait for a keystroke after finishing before exiting


## Config File Format

A basic file is included. The file has up four sections. Each section is structured like:

/*sectiontitle

...

sectiontitle*/


Anything not found between these constructs will be ignores. Valid section names are jslint, global, exclude, and sharplinter. An example:

// set global options

/*jslint 
	browser: true, 
	wsh: true, 
	laxbreak:true,
	evil: true,
	eqeqeq: true,
	curly: true,
	forin: true,
	immed: true,
	newcap: true,
	nomen: false,
	onevar: true,
	undef: true,
	jquery: true
	
jslint*/

// define some globals 

/*global 
	mynamespace,alert,confirm	
global*/

// define paths for file exclusions. Wild cards only work for the file name part of the path, not the 
// folders. Pattern matching within the path is therefore very simple, but wildcard matching for file names works

/*exclude

*.min.js
/temp/
/test/
/google-tracker.js
/thirdp/

exclude*/

// this section has only three options, as per below. The first two define a construct for ignoring blocks
// within files. The text used, when found tightly wrapped in a comment block like:
   /*ignore-start*/
       //...
   /*ignore-end*/
// will cause everything between the markers to be ignored.
// The final option, ignorefile, if found anywhere in a script will cause the entire file to be ignored.

/*sharplinter

ignorestart: ignore-start,
ignoreend: ignore-end,
ignorefile: lint-ignore-file

sharplinter*/

# Visual Studio Integration

SharpLinter produces output that Visual Studio can use to take you to the file and line associated with each error. It's not an extension, so you need
to configure it as an external tool. Do the following:

1. Go to Tools -> External Tools
2. Create a new tool with title "SharpLinter" (or whatever you like).
3. Set "Command" to the path of the executable.
4. Set arguments to something like this:

    -y -i $(ItemPath) -c "path/to/my/config" 

5. Check "Output Window"

You are now done. You can lint a file with Tools-> SharpLinter. If you want to make life even easier, add a keyboard shortcut:

6. Go to Tools -> Options -> Environment -> Keyboard
7. Type "externalcommand" where it says "show commands containing:"
8. All the external commands appear as "Tools.ExternalCommand1" through "Tools.ExternalCommand20". These numbers are the ordinal order in which they appear in your list. Choose the right one.
9. Click on "Press shortcut keys" and type whatever you want. I like Alt+z. You may have to remove it from something else first.

You can also set it up to process all the files in your project. Make another entry in External Tools, but instead of specifying the active file, get rid of the -i option and add this instead:

    -rd $(ProjectDir)/scripts/*.js -x *.min.js -x /thirdparty/

for example, to process all files in the "scripts" subfolder of your project, but skip the folder "thirdparty" within "scripts" and don't try to process minimized files.

