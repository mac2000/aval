aval
====

Parse credit card transactions report from aval bank

Main idea is to run this program from scheduler each ten minutes to get notifications about credit card transactions, and each day with `sendmail` key to get overall report.

First wersion was written in powershell script using internet explorer com object, but unfortunatelly com objects can not be accessed from scheduled task without complex manipulations over them.

Usage
-----

    aval.exe https://<username>:<password>@vipiska.aval.ua http://<username>:<password>@gmail.com [verbose] [sendmail]
