# Voice helper C# Project

> Voice-controlled computer management program for the Windows operating system. Enables launching and closing applications, opening folders.

> Программа для голосового управления компьютером под операционной системой Windows. Позволяет запускать и закрывать программы, открывать папки.

 ***Работает Offline - не требует интернета!***

[Как использовать см. на YouTube](https://youtu.be/GhmsrFBdQQg?si=CSigJrpOBp8XNLuk)

___
# Добавил параметр в settings.xml
_Спасибо @pillowanalyst, подписчику моего канала YouTube, за инициативу предложить изменения, которые были внесены в этот проект. Я очень ценю вашу обратную связь!"_

> Новый параметр ***minimized***

    <minimized>0</minimized> - запустить программу и показать пользователю окно программы
    <minimized>1</minimized> - запустить программу и скрыть окно программы в трее

> Новый параметр ***assistantVoiceName***

    Все установленные голоса на вашем компьютере будут отображены при запуске программы,
    и вы сможете скопировать наименование голоса в файл settings.xml, а именно в раздел assistantVoiceName.
    Например: <assistantVoiceName>Microsoft Irina Desktop - Russian</assistantVoiceName>.
    Если оставить поле пустым, то будет использован голос, установленный в соответствии с инструкцией (MSSpeech_TTS_ru-RU_Elena.msi).

> Новый параметр ***assistantVoiceRate***

    Теперь в файле настроек вы можете задать скорость, с которой читает голосовой ассистент,
    используя параметр assistantVoiceRate. Диапазон значений от 0 до 9. Например: <assistantVoiceRate>1</assistantVoiceRate>.
___
