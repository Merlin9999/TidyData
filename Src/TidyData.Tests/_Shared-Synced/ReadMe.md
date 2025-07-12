Changes to files in this folder are synced between GitHub repos when the changes are 
on the develop branch are pushed to GitHub. The affected repositories are:

TidyData and 
TidyData.Azure

A GitHub Action called Sync Files is run in the repository where the content is pushed. 
That action checks this folder for changes and creates a pull request in the other
repository reflecting the changes. 

That pull request must be manually merged.

A future improvement may include automatically merging these pull requests.
