(function()
{
   const storage   = window.localStorage;
   const savedDark = storage.getItem('dark');
   const switcher  = document.createElement("button");
   
   function changeMode()
   {
      const css = document.querySelectorAll("link[href='/css/dashboard.css']");

      if(css.length > 0)
      {
         css.forEach(c => c.remove());
         switcher.textContent = 'dark';
         
         return false;
      }
      else
      {
         let link = document.createElement("link");
         link.rel = "stylesheet";
         link.href = "/css/dashboard.css";
         
         switcher.textContent = 'white';

         document.body.appendChild(link);
         return true;
      }
   }

   switcher.className = "btn btn-default";
   switcher.setAttribute('style', "position: fixed; bottom: 10px; right: 10px;");
   switcher.append('dark');

   switcher.onclick = () => storage.setItem('dark', changeMode().toString());

   document.body.appendChild(switcher);
   
   if(savedDark === 'true')
      changeMode();
})();