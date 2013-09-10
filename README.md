Пример для WindowsCamp.ru
=====================

Это исходный код примера игры для доклада "Разработка игр для Windows 8 и Windows Phone. Monogame и cocos2d"
http://events.techdays.ru/WindowsCamp/2013-09/#agenda

Игра портирована со стандартного примера Crystal Craze для Cocos2d-x JS. 


Требования
-------------------------

1. Установленный Monogame 3.0 и выше http://monogame.net/downloads http://monogame.codeplex.com/releases/view/102870
2. Установленный Xamarin.Android http://xamarin.com/download (есть пробная версия на 30 дней)
3. Visual Studio 2012, Windows 8

Запуск
-------------------------

Откройте Craze1/Craze1.sln и соберите все решение (Rebuild solution).

Далее, запускайте любой проект, который вам интересен - Craze1 (WP7), Craze1.Android (Android) или Craze1.Store (Windows 8 Store/Metro)

Что такое Crystal Craze?
-------------------------

Crystal Craze это хрестоматийный пример Cocos2d-x на JavaScript.
Рабочий пример игры можно увидеть тут: http://drmtm.us/crazejs

Исходный код этого примера можно скачать по ссылке: [Crystal Craze JS source code sample](http://cocosbuilder.googlecode.com/files/CocosBuilder-3.0-alpha4-examples.zip)


Суть игры - набрать максимальное количество очков за определенное время (в оригинале 1 минута, в этом примере меньше. Смотреть класс Constants)
Нужно убирать с поля группы одинаковых кристаллов размером в 3 и больше.
Последовательное удаление 5+ групп включает режим Power Play и игрок получает дополнительные очки.
Также есть специальные кристаллы - бомбы. Как они действуют, понятно из названия :)

Все благодарности и права на идею игры принадлежат авторам примера CrystalCrazeJS:  

```
NASA, ESA, R. O'Connell (University of Virginia), F. Paresce (National Institute for Astrophysics, Bologna, Italy), E. Young (Universities Space Research Association/Ames Research Center), the WFC3 Science Oversight Committee, and the Hubble Heritage Team (STScI/AURA)
```
  
Вопросы
-------------------------

Вопросы можно задавать в твиттере [@AlexSorokoletov](http://twitter.com/alexsorokoletov)



Спасибо.

We deliver unique apps
http://dreamteam-mobile.com/apps/
http://dreamteam-mobile.com/blog/
