let SessionLoad = 1
let s:so_save = &g:so | let s:siso_save = &g:siso | setg so=0 siso=0 | setl so=-1 siso=-1
let v:this_session=expand("<sfile>:p")
silent only
silent tabonly
cd ~/Programming/Calculator
if expand('%') == '' && !&modified && line('$') <= 1 && getline(1) == ''
  let s:wipebuf = bufnr('%')
endif
let s:shortmess_save = &shortmess
if &shortmess =~ 'A'
  set shortmess=aoOA
else
  set shortmess=aoO
endif
badd +28 term://~//7382:/usr/bin/bash
badd +48 Calculator.Tests/Test1.cs
badd +100 Calculator/Parsing/Lexer.cs
badd +234 Calculator/Parsing/Parser.cs
badd +853 term://~/Programming/Calculator/Calculator.Tests//34260:/usr/bin/bash
badd +63 Calculator/Program.cs
badd +1 /\$metadata\$/Project/Calculator/Assembly/System/Collections/Symbol/System/Collections/Generic/List\`1.cs
argglobal
%argdel
set stal=2
tabnew +setlocal\ bufhidden=wipe
tabrewind
edit Calculator.Tests/Test1.cs
let s:save_splitbelow = &splitbelow
let s:save_splitright = &splitright
set splitbelow splitright
wincmd _ | wincmd |
vsplit
1wincmd h
wincmd w
let &splitbelow = s:save_splitbelow
let &splitright = s:save_splitright
wincmd t
let s:save_winminheight = &winminheight
let s:save_winminwidth = &winminwidth
set winminheight=0
set winheight=1
set winminwidth=0
set winwidth=1
exe 'vert 1resize ' . ((&columns * 117 + 118) / 236)
exe 'vert 2resize ' . ((&columns * 118 + 118) / 236)
argglobal
balt Calculator/Parsing/Lexer.cs
setlocal foldmethod=manual
setlocal foldexpr=0
setlocal foldmarker={{{,}}}
setlocal foldignore=#
setlocal foldlevel=0
setlocal foldminlines=1
setlocal foldnestmax=20
setlocal foldenable
silent! normal! zE
let &fdl = &fdl
let s:l = 52 - ((27 * winheight(0) + 28) / 56)
if s:l < 1 | let s:l = 1 | endif
keepjumps exe s:l
normal! zt
keepjumps 52
normal! 0
lcd ~/Programming/Calculator/Calculator.Tests
wincmd w
argglobal
if bufexists(fnamemodify("term://~//7382:/usr/bin/bash", ":p")) | buffer term://~//7382:/usr/bin/bash | else | edit term://~//7382:/usr/bin/bash | endif
if &buftype ==# 'terminal'
  silent file term://~//7382:/usr/bin/bash
endif
balt ~/Programming/Calculator/Calculator.Tests/Test1.cs
setlocal foldmethod=manual
setlocal foldexpr=0
setlocal foldmarker={{{,}}}
setlocal foldignore=#
setlocal foldlevel=0
setlocal foldminlines=1
setlocal foldnestmax=20
setlocal foldenable
let s:l = 1307 - ((55 * winheight(0) + 28) / 56)
if s:l < 1 | let s:l = 1 | endif
keepjumps exe s:l
normal! zt
keepjumps 1307
normal! 03|
wincmd w
2wincmd w
exe 'vert 1resize ' . ((&columns * 117 + 118) / 236)
exe 'vert 2resize ' . ((&columns * 118 + 118) / 236)
tabnext
edit ~/Programming/Calculator/Calculator/Parsing/Lexer.cs
let s:save_splitbelow = &splitbelow
let s:save_splitright = &splitright
set splitbelow splitright
wincmd _ | wincmd |
vsplit
1wincmd h
wincmd w
wincmd _ | wincmd |
split
1wincmd k
wincmd w
let &splitbelow = s:save_splitbelow
let &splitright = s:save_splitright
wincmd t
let s:save_winminheight = &winminheight
let s:save_winminwidth = &winminwidth
set winminheight=0
set winheight=1
set winminwidth=0
set winwidth=1
exe 'vert 1resize ' . ((&columns * 117 + 118) / 236)
exe '2resize ' . ((&lines * 27 + 29) / 59)
exe 'vert 2resize ' . ((&columns * 118 + 118) / 236)
exe '3resize ' . ((&lines * 28 + 29) / 59)
exe 'vert 3resize ' . ((&columns * 118 + 118) / 236)
argglobal
balt term://~/Programming/Calculator/Calculator.Tests//34260:/usr/bin/bash
setlocal foldmethod=manual
setlocal foldexpr=0
setlocal foldmarker={{{,}}}
setlocal foldignore=#
setlocal foldlevel=0
setlocal foldminlines=1
setlocal foldnestmax=20
setlocal foldenable
silent! normal! zE
let &fdl = &fdl
let s:l = 168 - ((28 * winheight(0) + 28) / 56)
if s:l < 1 | let s:l = 1 | endif
keepjumps exe s:l
normal! zt
keepjumps 168
normal! 030|
lcd ~/Programming/Calculator/Calculator
wincmd w
argglobal
if bufexists(fnamemodify("term://~/Programming/Calculator/Calculator.Tests//34260:/usr/bin/bash", ":p")) | buffer term://~/Programming/Calculator/Calculator.Tests//34260:/usr/bin/bash | else | edit term://~/Programming/Calculator/Calculator.Tests//34260:/usr/bin/bash | endif
if &buftype ==# 'terminal'
  silent file term://~/Programming/Calculator/Calculator.Tests//34260:/usr/bin/bash
endif
balt ~/Programming/Calculator/Calculator/Parsing/Lexer.cs
setlocal foldmethod=manual
setlocal foldexpr=0
setlocal foldmarker={{{,}}}
setlocal foldignore=#
setlocal foldlevel=0
setlocal foldminlines=1
setlocal foldnestmax=20
setlocal foldenable
let s:l = 853 - ((26 * winheight(0) + 13) / 27)
if s:l < 1 | let s:l = 1 | endif
keepjumps exe s:l
normal! zt
keepjumps 853
normal! 03|
lcd ~/Programming/Calculator/Calculator
wincmd w
argglobal
if bufexists(fnamemodify("~/Programming/Calculator/Calculator/Program.cs", ":p")) | buffer ~/Programming/Calculator/Calculator/Program.cs | else | edit ~/Programming/Calculator/Calculator/Program.cs | endif
if &buftype ==# 'terminal'
  silent file ~/Programming/Calculator/Calculator/Program.cs
endif
balt ~/Programming/Calculator/Calculator/Parsing/Parser.cs
setlocal foldmethod=manual
setlocal foldexpr=0
setlocal foldmarker={{{,}}}
setlocal foldignore=#
setlocal foldlevel=0
setlocal foldminlines=1
setlocal foldnestmax=20
setlocal foldenable
silent! normal! zE
let &fdl = &fdl
let s:l = 63 - ((11 * winheight(0) + 14) / 28)
if s:l < 1 | let s:l = 1 | endif
keepjumps exe s:l
normal! zt
keepjumps 63
normal! 041|
lcd ~/Programming/Calculator/Calculator
wincmd w
exe 'vert 1resize ' . ((&columns * 117 + 118) / 236)
exe '2resize ' . ((&lines * 27 + 29) / 59)
exe 'vert 2resize ' . ((&columns * 118 + 118) / 236)
exe '3resize ' . ((&lines * 28 + 29) / 59)
exe 'vert 3resize ' . ((&columns * 118 + 118) / 236)
tabnext 1
set stal=1
if exists('s:wipebuf') && len(win_findbuf(s:wipebuf)) == 0 && getbufvar(s:wipebuf, '&buftype') isnot# 'terminal'
  silent exe 'bwipe ' . s:wipebuf
endif
unlet! s:wipebuf
set winheight=1 winwidth=20
let &shortmess = s:shortmess_save
let &winminheight = s:save_winminheight
let &winminwidth = s:save_winminwidth
let s:sx = expand("<sfile>:p:r")."x.vim"
if filereadable(s:sx)
  exe "source " . fnameescape(s:sx)
endif
let &g:so = s:so_save | let &g:siso = s:siso_save
doautoall SessionLoadPost
unlet SessionLoad
" vim: set ft=vim :
